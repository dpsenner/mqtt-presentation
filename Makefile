MQTT_BROKER_HOST = 10.10.10.60
OUT_FORMAT = beamer
THEME = -V theme:default -V colortheme:crane -V fonttheme:structurebold
PANDOC_OPTS = -t $(OUT_FORMAT) $(THEME) --slide-level 2

.PHONY: all clean

all: PRESENTATION.md

PRESENTATION.md:
	cd src && pandoc $(PANDOC_OPTS) -o PRESENTATION.pdf PRESENTATION.md
	evince src/PRESENTATION.pdf

publish_temperature:
	-src/publish_temperature/client.sh $(MQTT_BROKER_HOST)

subscribe_temperature:
	-mosquitto_sub -h $(MQTT_BROKER_HOST) -v -t '#'

clean:
	-@rm -f src/*.pdf
