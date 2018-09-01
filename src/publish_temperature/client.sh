#!/bin/bash
BROKER="$1"
if [ -z "$BROKER" ];
then
	BROKER="localhost"
fi
PUBLISH="mosquitto_pub"
CLIENT_ID=`hostname`

while true;
do
	sensors | grep "^temperature" | awk '{print substr($1,1,length($1)-1)" "substr($2,2)}' | while IFS= read -r line; do
		topic_suffix=$(echo $line | awk '{print $1}' | sed -e 's/^temperature\///')
		temperature=$(echo $line | awk '{print $2}')
		topic="$CLIENT_ID/property/temperature/$topic_suffix"
		$PUBLISH -h $BROKER -t $topic -m "$temperature"
		echo "$topic: $temperature"
	done
	sleep 1
done
