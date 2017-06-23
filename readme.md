# Goal
The goal of this project was to implement an app that will notify the user about interruptions at the Wiener Linien transport system. 
The User should have the possibility to register for some lines he wishes to observe, as well as register with his GPS coordinates and get notifications about all 
interruption which occur at those lines or are near him. 

#Implementation
We used Xamarin ar framework for the App. The App is sending User Settings voa EventHubs to an Azure cloud where the Sensor Data from Wiener Linien and is processed.
The result is then send back to the clients via IoTHubs. The client will display the results. 

#How to set up the Azure Cloud

## Add needed Blades

### Event Hub
Add an "Event Hub" blade to your Azure account.

### Service Bus
Add these 3 topics: 

	interruptionnotificationtopic
	registrationtopic
	userdatatopic


### IoT Hub
Add an "IoT Hub" blade to your Azure account. Add two endpoints. RegisterTopicEndpoint and UserDataEndpoint. 
Configure 2 Routes:

Name: UserDataRoute, From: DeviceMessages, Where: type="userdata", Into: UserDataEndpoint 
Name:RegisterTopicRoute, From: DeviceMessages, Where: type="register", Into: RegisterTopicEndpoint true

# Storage
Add a Storsage account
Whenever a Blade need a sotrage account provide it with the name of this one. 
Add a new Blob Container for the user reference data. Named: UserReferenceData

#Function Appp


#Stream Analytics