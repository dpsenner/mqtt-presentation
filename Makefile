MQTT_BROKER_HOST = mqtt.hta
OUT_FORMAT = beamer
THEME = -V theme:default -V colortheme:crane -V fonttheme:structurebold
PANDOC_OPTS = -t $(OUT_FORMAT) $(THEME) --slide-level 2

presentation:
	cd src && pandoc $(PANDOC_OPTS) -o PRESENTATION.pdf PRESENTATION.md
	evince src/PRESENTATION.pdf

chat-client:
	-src/chat_client/run.sh $(MQTT_BROKER_HOST)

chat-client-requirements:
	sudo apt install python3-pip
	sudo pip3 install virtualenv

publish-temperature-stateless:
	-src/publish_temperature_stateless/run.sh $(MQTT_BROKER_HOST)

publish-temperature-stateless-requirements:
	sudo apt install sensors mosquitto-clients

publish-temperature-stateful:
	-src/publish_temperature_stateful/run.sh $(MQTT_BROKER_HOST)

publish-temperature-stateful-requirements:
	sudo apt install python3-pip
	sudo pip3 install virtualenv

publish-temperature-stateful-rebirth-mugen:
	mosquitto_pub -h $(MQTT_BROKER_HOST) -t mugen/command/rebirth -m yes

publish-temperature-stateful-shutdown-mugen:
	mosquitto_pub -h $(MQTT_BROKER_HOST) -t mugen/command/shutdown -m yes

temperature-alarms:
	-dotnet run --project src/publish_temperature_alarms/PublishTemperatureAlarms/PublishTemperatureAlarms.csproj -- run -h $(MQTT_BROKER_HOST)

temperature-alarms-requirements:
	sudo apt install dotnet-sdk-2.1

temperature-alarms-temperature-threshold-low:
	mosquitto_pub -h $(MQTT_BROKER_HOST) -t temperature-alarm/property/temperature-threshold/set -m 30

temperature-alarms-temperature-threshold-normal:
	mosquitto_pub -h $(MQTT_BROKER_HOST) -t temperature-alarm/property/temperature-threshold/set -m 65

temperature-alarms-shutdown:
	mosquitto_pub -h $(MQTT_BROKER_HOST) -t temperature-alarm/command/shutdown -m very-secret

subscribe-all:
	-mosquitto_sub -h $(MQTT_BROKER_HOST) -v -t '#'
