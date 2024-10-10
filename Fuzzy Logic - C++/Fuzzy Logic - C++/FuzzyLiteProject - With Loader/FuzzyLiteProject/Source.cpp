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

	//output variable 1
	OutputVariable* mSteer = new OutputVariable;
	engine->addOutputVariable(mSteer);

	mSteer->setName("mSteer");
	mSteer->setRange(0.000, 1.000);
	mSteer->addTerm(new Triangle("left", 0.0, 0.3, 0.6));
	mSteer->addTerm(new Triangle("right", 0.4, 0.9, 1.0));
	mSteer->setDefuzzifier(new Centroid(100));

	mSteer->setAggregation(new Maximum);
	mSteer->setDefaultValue(fl::nan);

	//rule block
	RuleBlock* mamdani = new RuleBlock;
	mamdani->setName("mamdani");
	mamdani->setConjunction(fl::null);
	mamdani->setDisjunction(fl::null);
	mamdani->setImplication(new AlgebraicProduct);
	mamdani->setActivation(new General);

	mamdani->addRule(Rule::parse("if obstacle is left then mSteer is right", engine));
	mamdani->addRule(Rule::parse("if obstacle is right then mSteer is left", engine));
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
		std::cin >> input;

		std::stringstream ss(input);
		float number = 0.0f;
		ss >> number;

		obstacle->setValue(number);
		engine->process();
		
		std::cout << "obstacle.input = " << number << " => steer.output = " << mSteer->getValue() << std::endl;
	}





	//exit
	delete engine;
	return 0;
}
