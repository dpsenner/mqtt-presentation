#!/bin/bash
DIRNAME="`dirname "$0"`"
VENV="$DIRNAME/.venv"
MAIN="$DIRNAME/main.py"
ACTIVATE="$VENV/bin/activate"
if [ ! -d "$VENV" ];
then
    # first time installation
    virtualenv $VENV
    # activate venv
    source $ACTIVATE
    # install requirements
    pip3 install paho-mqtt
else
    source $ACTIVATE
fi

BROKER="$1"
if [ -z "$BROKER" ];
then
	BROKER="localhost"
fi

# run client
$MAIN --host $BROKER

# deactivate venv
deactivate