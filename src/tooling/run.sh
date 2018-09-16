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

# run client
$MAIN $@

# deactivate venv
deactivate