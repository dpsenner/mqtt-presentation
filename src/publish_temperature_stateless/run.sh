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
	is_sensor=0
	sensors | while IFS= read -r line; do
		if [[ $line == Adapter:* ]];
		then
			is_sensor=1
		elif [[ -z $line ]];
		then
			is_sensor=0
		elif [ $is_sensor -eq 1 ];
		then
			sensor=$(echo $line | awk '{print substr($1,1,length($1)-1)}')
			value=$(echo $line | awk '{print $2}' | sed -e 's/[^-\\.0-9]//g')
			unit=$(echo $line | awk '{if ($2 ~ /[0-9]$/) {print $3} else {print substr($2,length($2)-1,2)}}')
			topic="$CLIENT_ID/property/$sensor"
			message="$value"
			$PUBLISH -h $BROKER -t $topic -m "$message"
		fi
	done
	sleep 1
done
