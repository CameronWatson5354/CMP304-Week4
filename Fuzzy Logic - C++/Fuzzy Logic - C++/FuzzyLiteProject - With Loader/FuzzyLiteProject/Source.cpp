#include <iostream> 
#include "FlsImporter.h"
#include <fl/Headers.h>

using namespace fl;

int main()
{
	Engine* engine = new Engine();



	/*
	* TODO - Define input, output and rules
	*/
	//input variable 1
	InputVariable* obstacle = new InputVariable;
	engine->addInputVariable(obstacle);

	obstacle->setName("obstacle");
	obstacle->setRange(0.000, 1.000);
	obstacle->addTerm(new Triangle("left", 0.0, 0.05, 0.6));
	obstacle->addTerm(new Triangle("right", 0.4, 0.95, 1.0));

	//input variable 2
	InputVariable* speed = new InputVariable;
	engine->addInputVariable(speed);

	speed->setName("speed");
	speed->setRange(0.000, 1.000);
	speed->addTerm(new Triangle("movingLeft", 0.0, 0.0, 0.6));
	speed->addTerm(new Triangle("none", 0.3, 0.5, 0.7));
	speed->addTerm(new Triangle("movingRight", 0.4, 1.0, 1.0));


	//output variable 1
	OutputVariable* mSteer = new OutputVariable;
	engine->addOutputVariable(mSteer);

	mSteer->setName("mSteer");
	mSteer->setRange(0.000, 1.000);
	mSteer->addTerm(new Triangle("left", 0.0, 0.3, 0.6));
	mSteer->addTerm(new Triangle("right", 0.4, 0.7, 1.0));
	mSteer->setDefuzzifier(new Centroid(100));

	mSteer->setAggregation(new Maximum);
	mSteer->setDefaultValue(fl::nan);

	//rule block
	RuleBlock* mamdani = new RuleBlock;
	mamdani->setName("mamdani");
	mamdani->setConjunction(new AlgebraicProduct);
	mamdani->setDisjunction(new Maximum);
	mamdani->setImplication(new AlgebraicProduct);
	mamdani->setActivation(new General);

	/*mamdani->addRule(Rule::parse("if obstacle is left then mSteer is right", engine));
	mamdani->addRule(Rule::parse("if obstacle is right then mSteer is left", engine));*/

	mamdani->addRule(Rule::parse("if obstacle is left or speed is movingLeft then mSteer is right", engine));
	mamdani->addRule(Rule::parse("if obstacle is right or speed is movingRight then mSteer is left", engine));
	engine->addRuleBlock(mamdani);


	// Check the status and print error if engine not setup
	std::string status;
	if (not engine->isReady(&status))
		std::cout << "Engine is not ready" << std::endl << status << std::endl;


	/*
	* TODO - Add a gameplay loop
	*/
	while (true)
	{
		std::string input = "";
		std::stringstream ss(input);

		float number = 0.0f;
		float number2 = 0.0f;

		std::cin >> input;
		ss << input;
		ss >> number;
		ss.clear();

		std::cin >> input;
		ss << input;
		ss >> number2;

		obstacle->setValue(number);
		speed->setValue(number2);
		engine->process();
		
		std::cout << "obstacle.input = " << number << " => steer.output = " << mSteer->getValue() << std::endl;
	}





	//exit
	delete engine;
	return 0;
}
