#!/bin/bash
BROKER="$1"
if [ -z "$BROKER" ];
then
	BROKER="localhost"
fi
PUBLISH="mosquitto_pub"
CLIENT_ID=`hostname`
TEMPERATURE_CMD=sensors | grep '^/' | grep "Temperature" | awk '{print substr($1,0,length($1)-1)" "$2}'

while true;
do
	sensors | grep "/Temperature" | awk '{print substr($1,0,length($1)-1)" "substr($2,2)}' | while IFS= read -r line; do
		topic_suffix=$(echo $line | awk '{print $1}')
		temperature=$(echo $line | awk '{print $2}')
		topic="/$CLIENT_ID/$topic_suffix"
		$PUBLISH -h $BROKER -t $topic -m "$temperature"
		echo "$topic: $temperature"
	done
	sleep 1
done
