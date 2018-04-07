# Dodge
### A Windows 8.1 game applicaiton, with Linq to Xml and Windows Isolated Storage.
![Play Demo](https://github.com/PrisonerM13/Dodge/blob/master/gif/Play.gif "Play Demo")

#### Features/Technologies/Tools:
+ Windows 8.1 Universal App
+ AppBar/CommandBar
+ Windows Isolated Storage
+ Linq to Xml

#### Structure
The App consists of one project (my first project, remember? ;-))

Main Objects
+ MainPage
	+ Hosts the app's splash image, command bar (opens by right click), and Load/Save/Settings Canvas objects.
	+ Contains 2 grids: One for the game's board and one for indicators (timer and life counter).
	+ Contains the "Game" object
+ Game
	+ Controls the game's timer and level.
	+ Contains the game's Board and serves as a middleware between MainPage and Board object.
+ Board
	+ Hosts the game's entities (Player, Enemies and Obstacles) and controls their movement.
	+ Events: NewEntityCreated, EntityIsHit, EntityIsDead
+ Entity
	+ Abstract class that serves as a parent class for Player, Enemy and Obstacle classes.
	+ Events: PositionChanged, ImageOpened
	+ Properties as shown in the diagram below.
+ Player
	+ Inherits from class Entity
	+ Has a "step size" property, initialized in constructor.
	+ Has a Move method, allowing to change its position one step at a time, at a given direction.
+ Enemy
	+ Inherits from class Entity
	+ Has a "step size" property, initialized in constructor.
	+ Has a private Move method, called by a timer, whos interval determined according to game's level.
	+ Has the following randomly set properties, that controls the way it moves:
		
		Move Pattern
		
		| Pattern          | Description                                                                             |
		| ---------------- | --------------------------------------------------------------------------------------- |
		| Following Player | Moves towards player                                                                    |
		| Straight         | Moves back and forth in straight lines                                                  |
		| Diagonal         | Moves in diagonal lines; Changes direction in 90 degrees when hitting something         |
		| Circular         | Moves in circles. Changes direction (clockwise/counterclockwise) when hitting something. Circle's Radius is set to maximum possible on initial positioning |
		
		Move Pace
		
		| Pace        | Description                                                                                                                    |
		| ----------- | ------------------------------------------------------------------------------------------------------------------------------ |
		| Constant    | Moves one step every timer tick. Default for following-player enemies                                                          |
		| Quantum     | Stays in place for a predetermined number of timer ticks and then moves a predetermined number of steps                        |
		| Accelerated | Starts moving in a regular interval, then accelerates to a predetermined value, then returns to a regular interval and starts again |
		| Decelerated | Starts moving in a regular interval, then decelerates to a predetermined value, then returns to a regular interval and starts again |
		
![Entities](https://github.com/PrisonerM13/Dodge/blob/master/images/Entities.png "Entities")
		
#### Instructions
+ The player first appears in the middle of the screen and is marked with a bottom yellow "shadow".
+ The target is to avoid enemies until there's only one left, and to achieve that at a minimum time. 
+ Move the player with the keyboard's arrows.
+ A collision between a player and an enemy decrements both their lifes by 1.
+ A collision between two enemies decrements the life of the causer by 1. 
+ Player lifes: 10
+ Enemy lifes: 3

#### Menu Operations
> Use mouse right click to show/hide menu bar
+ Settings
	+ Level
		+ Beginner (default)
		+ Intermediate
		+ Expert
		
	The game's level controls the speed in which enemies move.		
		
	![Settings](https://github.com/PrisonerM13/Dodge/blob/master/gif/Settings.gif "Settings")

+ Save/Load Game
	+ Use Save/Load buttons at top command bar
	+ Files are saved as XML and are stored in:
		%LOCALAPPDATA%\Packages\\{GUID}\LocalState
		
	![Save And Load](https://github.com/PrisonerM13/Dodge/blob/master/gif/SaveAndLoad.gif "Save And Load")
