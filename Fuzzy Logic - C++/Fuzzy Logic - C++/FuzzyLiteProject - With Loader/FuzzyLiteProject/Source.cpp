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





	// Check the status and print error if engine not setup
	std::string status;
	if (not engine->isReady(&status))
		std::cout << "Engine is not ready" << std::endl << status << std::endl;


	/*
	* TODO - Add a gameplay loop 
	*/






	//exit
	delete engine;
	return 0;
}
