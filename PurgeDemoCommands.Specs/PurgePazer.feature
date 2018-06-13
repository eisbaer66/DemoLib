Feature: PurgePazer
	In order to view demos without consolecomands
	As a TF2 player
	I want to remove consolecomands from demo-files

@ConsoleTest
Scenario: purge single demo
	Given the arguments [TestData\_\0-Me-Intro.dem]
	And the timeout 2000
	When I run PurgeDemoComands
	Then I expect files
	 | TestData\_\purged\0-Me-Intro.dem |
	 
@ConsoleTest
Scenario: purge multiple demos
	Given the arguments [TestData\_\0-Me-Intro.dem TestData\_\19-Me-Using-Other-Spies-As-Decoy.dem]
	And the timeout 4000
	When I run PurgeDemoComands
	Then I expect files
	 | TestData\_\purged\0-Me-Intro.dem |
	 | TestData\_\purged\19-Me-Using-Other-Spies-As-Decoy.dem |
	 
@ConsoleTest
Scenario: purge folder '_'
	Given the arguments [TestData\_]
	And the timeout 2000
	When I run PurgeDemoComands
	Then I expect files
	 | TestData\_\purged\0-Me-Intro.dem |
	 | TestData\_\purged\19-Me-Using-Other-Spies-As-Decoy.dem |
	 
@ConsoleTest
Scenario: purge folder '1'
	Given the arguments [TestData\1]
	And the timeout 30000
	When I run PurgeDemoComands
	Then I expect files
	 | TestData\1\purged\0-Me-Intro.dem |
	 | TestData\1\purged\1-Intro-Commander-Snowcat.dem |
	 | TestData\1\purged\2-Losing-the-spycicle-Rattlewrench.dem |
	 | TestData\1\purged\3-Blob-Attacking-Spy-Payload.dem |
	 | TestData\1\purged\4-Turbine-Mirkiens.dem |
	 | TestData\1\purged\5-Nice-Work-By-Kenpachi.dem |
	 | TestData\1\purged\6-Things-did-not-go-well-for-Palex-5CP.dem |
	 | TestData\1\purged\7-Things-went-a-little-better-for-Palxex-5CP.dem |
	 | TestData\1\purged\8-Tricking-Medic-In-To-Sentry-Fire-Palexlife-tick-5000.dem |
	 | TestData\1\purged\9-too-many-kritz-Commander-snowcat.dem |
	 | TestData\1\purged\10-Amadeus-Distrupting-The-Engineers.dem |
	 | TestData\1\purged\11-Amadeus-Competetive.dem |
	 | TestData\1\purged\12-Passive-Spy-Reptomansam.dem |
	 | TestData\1\purged\13-Dochnicht-weakness-of-dead-ringer.dem |
	 | TestData\1\purged\14-TheDop-Quick-And-Easy.dem |
	 | TestData\1\purged\15-Me-Capping-5CP.dem |
	 | TestData\1\purged\16-Me-Time-To-Death-From-Stab-On-Soldier-To-Scout-Killed.dem |
	 | TestData\1\purged\17-Me-Being-Clever-With-Teleporters.dem |
	 | TestData\1\purged\18-Me-The-Bullets-Curve.dem |
	 | TestData\1\purged\19-Me-Using-Other-Spies-As-Decoy.dem |
	 
@ConsoleTest
Scenario: purge folder '2'
	Given the arguments [TestData\2]
	And the timeout 14000
	When I run PurgeDemoComands
	Then I expect files
	 | TestData\2\purged\bodyblock.dem |
	 | TestData\2\purged\episode3.dem |
	 | TestData\2\purged\frontalassault.dem |
	 | TestData\2\purged\HOW_TO_PAYLOAD.dem |
	 | TestData\2\purged\mannofsteel.dem |
	 | TestData\2\purged\powergunner.dem |
