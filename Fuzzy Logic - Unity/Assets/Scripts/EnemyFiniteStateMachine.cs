using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FLS;
using FLS.Rules;
using FLS.MembershipFunctions;
using System;


public class EnemyFiniteStateMachine : MonoBehaviour {


	//Fuzzy Logic Components
	IFuzzyEngine engine; // the Fuzzy Logic Engine, which uses the FLS Library
	LinguisticVariable distance; // the input variable - distance to the player
	LinguisticVariable speed; // the output variable - speed multiplier for the enemmies
	LinguisticVariable playerSpeed;


    public float patrolSpeed = 1f;
	public float chaseSpeed = 8f;
	public float triggerRange = 7.5f;
	bool facingRight = true;
	Animator anim;
	bool movementAllowed = true;
	private float speedMultiplier = 1f;

	bool grounded = false;
	public GameObject groundCheck;
	float groundRadius = 0.2f;
	public LayerMask whatIsGround;

	public GameObject stateBanner;
	public Sprite chaseSprite;
	public Sprite patrolSprite;
	public Sprite deadSprite;
	public Sprite waitSprite;
	private SpriteRenderer bannerSpriteRenderer;

	private enum State {Patrol = 0, Chase = 1, Dead = 2, Wait = 3};
	private State aiState = State.Patrol;

	GameObject player;
	private static float baseSpeed = 3.0f;



	// Use this for initialization
	void Start () {
		anim = GetComponent<Animator>();
		player = GameObject.Find("Player");
		bannerSpriteRenderer = stateBanner.GetComponent<SpriteRenderer>();

		InitFuzzyInferenceSystems();

    }

    /**
	 * InitFuzzyInferenceSystems will initialise the Fuzzy Engine
	 * Should be called once to set up the system
	 */
    private void InitFuzzyInferenceSystems()
	{
		// input variable - the distance beteween the player 
		// we use four terms - close, medium, far and distant
        distance = new LinguisticVariable("distance");
        var close = distance.MembershipFunctions.AddTrapezoid("close", 0, 0, 1, 3);
        var medium = distance.MembershipFunctions.AddTrapezoid("medium", 1, 2, 3, 4);
        var far = distance.MembershipFunctions.AddTrapezoid("far", 2, 6, 10, 12);
        var distant = distance.MembershipFunctions.AddTrapezoid("distant", 8, 12, 20, 20);

		//input variable -> speed
		playerSpeed = new LinguisticVariable("playerSpeed");
		var slowPlayerSpeed = playerSpeed.MembershipFunctions.AddTrapezoid("slowPlayerSpeed", 0.0, 0.0, 1.0, 2.0);
		var mediumPlayerSpeed = playerSpeed.MembershipFunctions.AddTrapezoid("mediumPlayerSpeed", 1.0, 2.0, 5.0, 6.0);
		var fastPlayerSpeed = playerSpeed.MembershipFunctions.AddTrapezoid("fastPlayerSpeed", 4.0, 7.0, 10.0, 10.0);

		// output variable - the chase speed multiplier
		// we use three terms - slow, medium and fast
        speed = new LinguisticVariable("chase");
        var slowSpeed = speed.MembershipFunctions.AddTrapezoid("slowSpeed", 0.2, 0.4, 0.6, 2.0);
        var mediumSpeed = speed.MembershipFunctions.AddTrapezoid("mediumSpeed", 0.5, 0.8, 1.2, 1.5);
        var fastSpeed = speed.MembershipFunctions.AddTrapezoid("fastSpeed", 1, 1.2, 5.0, 5.0);

		// define the rules used by the engine
        engine = new FuzzyEngineFactory().Default();
        var rule1 = Rule.If(distance.Is(close).Or(playerSpeed.Is(fastPlayerSpeed))).Then(speed.Is(slowSpeed));
        var rule2 = Rule.If(distance.Is(medium)).Then(speed.Is(mediumSpeed));
        var rule3 = Rule.If(distance.Is(far)).Then(speed.Is(fastSpeed));

		//player speed rules
		var rule4 = Rule.If(playerSpeed.Is(slowPlayerSpeed)).Then(speed.Is(slowSpeed));
		var rule5 = Rule.If(playerSpeed.Is(mediumPlayerSpeed)).Then(speed.Is(mediumSpeed));
		var rule6 = Rule.If(playerSpeed.Is(fastPlayerSpeed).Or(distance.Is(far))).Then(speed.Is(fastSpeed));

		// add the rules to the engine
		engine.Rules.Add(rule1);
		engine.Rules.Add(rule2);
		engine.Rules.Add(rule3);

		//engine.Rules.Add(rule4);
		//engine.Rules.Add(rule5);
		//engine.Rules.Add(rule6);

	}
	public void StopBots()
	{
        chaseSpeed = 0f;
		patrolSpeed = 0f;
		triggerRange = 0f;
		movementAllowed = false;
		Rigidbody2D rigidBodyComp = this.GetComponent<Rigidbody2D>();
		rigidBodyComp.velocity = new Vector2(0f, rigidBodyComp.velocity.y);
		anim.SetFloat("speed", 0f);
		aiState = State.Wait; 
	}

	// Called at set interval each time. Good for physics
	void FixedUpdate()
	{
		
		BaseMovementAndAnimation();

		switch(aiState)
		{
		case State.Chase:
			ChasePlayer();
			bannerSpriteRenderer.sprite = chaseSprite;
			break;
		case State.Patrol:
			PerformPatrol();
			bannerSpriteRenderer.sprite = patrolSprite;
			break;
		case State.Dead:
			bannerSpriteRenderer.sprite = deadSprite;
			break;
		case State.Wait:
			bannerSpriteRenderer.sprite = waitSprite;
			break;
		default:
			break;
		}

	}

	private void BaseMovementAndAnimation()
	{
		Rigidbody2D rigidBodyComp = GetComponent<Rigidbody2D>();

		grounded = Physics2D.OverlapCircle(groundCheck.transform.position, groundRadius, whatIsGround);
		anim.SetBool("ground", grounded);

		anim.SetFloat("vSpeed", rigidBodyComp.velocity.y);

		if((facingRight && (rigidBodyComp.velocity.x < 0f)) || (!facingRight && (rigidBodyComp.velocity.x > 0f)))
		{
			FlipTransform();
		}

        if (movementAllowed)
        {
			if(aiState== State.Chase)
			{
				// use the output from the fuzzy engine to adjust the speed
				// maxSpeed controls the speed at which the enemy moves
				// - direction is important (+ve or -ve) 
				float fuzzyMultiplier  = getSpeedMultiplierForDistance(GetHorizontalDistance(), player.GetComponent<Rigidbody2D>().velocity.x);
				Debug.Log(fuzzyMultiplier);
				//Debug.Log(player.GetComponent<Rigidbody2D>().velocity.x);

				if(chaseSpeed > 0)
                    chaseSpeed = baseSpeed * fuzzyMultiplier;
				else
                    chaseSpeed = -baseSpeed * fuzzyMultiplier;

            }

            PerformMovement(aiState == State.Chase ? chaseSpeed : patrolSpeed);
        }


		if(rigidBodyComp.position.y <= -5.5f)
		{
			aiState = State.Dead;
			Dead();
		}
	}

	void PerformPatrol()
	{
		Vector3 offset = new Vector3(patrolSpeed, 0f);
		bool grounded = Physics2D.OverlapCircle(groundCheck.transform.position + offset, groundRadius, whatIsGround);
		if(!grounded)
		{
			FlipMovement();
		}

        if((this.transform.position - player.transform.position).magnitude < triggerRange)
		{
			aiState = State.Chase;
		}

    }



	/**
	 * getSpeedMultiplierForDistance will the fuzzy logic engine to calculate and return a multiplier based on parameter distance
	 */
	float getSpeedMultiplierForDistance(float distance, float playerSpeed)
	{ 
		//we pass in the absolute distance as our input, it does not matter if the player is left or right of the enemy
		return (float)engine.Defuzzify(new { playerSpeed = (double)Math.Abs(playerSpeed), distance =  (double)Math.Abs(distance) }); 
    }

	float GetHorizontalDistance()
	{
        return this.transform.position.x - player.transform.position.x;
    }

	void ChasePlayer()
	{
		float horizontalDifference = GetHorizontalDistance();

		if(((chaseSpeed > 0f) && (horizontalDifference > 0f)) || ((chaseSpeed < 0f) && (horizontalDifference < 0f)))
		{
			speedMultiplier = 3f;		
			FlipMovement();
		}

		if((this.transform.position - player.transform.position).magnitude > triggerRange*1.5f)
		{
			aiState = State.Patrol;
		}
	}

	void FlipMovement()
	{
		patrolSpeed *= -1;
        chaseSpeed *= -1;
	}

	void FlipTransform()
	{
		speedMultiplier = 1f;

		facingRight = !facingRight;
		Vector3 scale = transform.localScale;
		scale.x *= -1;
		transform.localScale = scale;

		Vector3 bannerScale = stateBanner.transform.localScale;
		bannerScale.x *= -1;
		stateBanner.transform.localScale = bannerScale;
	}

	void PerformMovement(float speed)
	{
		float absSpeed = Mathf.Abs(speed);
		Rigidbody2D rigidBodyComp = GetComponent<Rigidbody2D>();
		float newXVelocity = rigidBodyComp.velocity.x + speed*speedMultiplier*Time.fixedDeltaTime;
		rigidBodyComp.velocity = new Vector2(Mathf.Clamp(newXVelocity, -absSpeed, absSpeed), rigidBodyComp.velocity.y);
		anim.SetFloat("speed", speed/ chaseSpeed);
	}

	private void Dead()
	{
		anim.SetBool("dead", true);
		this.GetComponent<Rigidbody2D>().gravityScale = 0f;
		Destroy(this.GetComponent<Collider2D>());
		movementAllowed = false;
	}
}
