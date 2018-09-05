MQTT_BROKER_HOST = 10.10.10.60
OUT_FORMAT = beamer
THEME = -V theme:default -V colortheme:crane -V fonttheme:structurebold
PANDOC_OPTS = -t $(OUT_FORMAT) $(THEME) --slide-level 2

presentation:
	cd src && pandoc $(PANDOC_OPTS) -o PRESENTATION.pdf PRESENTATION.md
	evince src/PRESENTATION.pdf

run-publish-temperature-sh:
	-src/publish_temperature/client.sh $(MQTT_BROKER_HOST)

run-publish-temperature-py:
	-src/publish_temperature/client.py --host $(MQTT_BROKER_HOST)

run-temperature-alarm:
	-dotnet run --project src/publish_temperature_alarms/PublishTemperatureAlarms/PublishTemperatureAlarms.csproj -- run -h $(MQTT_BROKER_HOST)

set-temperature-alarm-cpu-threshold-low:
	mosquitto_pub -h $(MQTT_BROKER_HOST) -t temperature-alarm/property/cpu-threshold/set -m 30

set-temperature-alarm-cpu-threshold-normal:
	mosquitto_pub -h $(MQTT_BROKER_HOST) -t temperature-alarm/property/cpu-threshold/set -m 65

shutdown-temperature-alarm:
	mosquitto_pub -h $(MQTT_BROKER_HOST) -t temperature-alarm/command/shutdown -m very-secret

subscribe-all:
	-mosquitto_sub -h $(MQTT_BROKER_HOST) -v -t '#'

clean:
	-@rm -f src/*.pdf
