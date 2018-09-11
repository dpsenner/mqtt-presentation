#!/usr/bin/env python3
import curses
import json
import paho.mqtt.client as mqtt
import re

class CommandHistorian:
    def __init__(self):
        self._items = []
        self._index = 0
    
    def navDown(self):
        self._index -= 1
        if self._index < 0:
            self._index = len(self._items) - 1
        if self._index < 0:
            self._index = 0
    
    def navUp(self):
        self._index += 1
        if self._index >= len(self._items):
            self._index = 0

    def push(self, item):
        self._items.insert(self._index, item)
    
    def put(self, item):
        self._items[self._index] = item
    
    def get(self):
        return self._items[self._index]


class ChatUI:
    def __init__(self, stdscr):
        self._stdscr = stdscr
        self._inputbuffer = ""
        self._chatbuffer = []
        self._inputhistory = CommandHistorian()

        self._chatbuffer_window = stdscr.derwin(curses.LINES - 2, curses.COLS, 0, 0)
        self._inputbuffer_window = stdscr.derwin(1, curses.COLS, curses.LINES - 1, 0)
        self._render()

    def readinput(self, prefix):
        self._inputbuffer = prefix
        self._render_inputbuffer()
        self._inputbuffer_window.cursyncup()
        self._inputhistory.push("")
        while True:
            i = self._stdscr.getch()
            if i == ord('\n'):
                result = self._inputbuffer[len(prefix):]
                self._inputbuffer = ""
                self._render_inputbuffer()
                self._inputbuffer_window.cursyncup()
                return result
            elif i in [curses.KEY_BACKSPACE]:
                if self._inputbuffer == prefix:
                    # do not accept backspace
                    continue
                # remove last char from inputbuffer
                self._inputbuffer = self._inputbuffer[:-1]
                self._inputhistory.put(self._inputbuffer[len(prefix):])
                self._render_inputbuffer()
            elif i == curses.KEY_RESIZE:
                self._resize()
            elif i == curses.KEY_DOWN:
                # navigate in the input history
                self._inputhistory.navDown()
                command = self._inputhistory.get()
                self._inputbuffer = "{0}{1}".format(prefix, command)
                self._render_inputbuffer()
            elif i == curses.KEY_UP:
                # navigate in the input history
                self._inputhistory.navUp()
                command = self._inputhistory.get()
                self._inputbuffer = "{0}{1}".format(prefix, command)
                self._render_inputbuffer()
            elif 32 <= i <= 126:
                # printable char
                self._inputbuffer += chr(i)
                self._inputhistory.put(self._inputbuffer[len(prefix):])
                self._render_inputbuffer()
    
    def chatbuffer_addmessage(self, author, message):
        self._chatbuffer.append((author, message))
        self._render()
        self._inputbuffer_window.cursyncup()
    
    def _resize(self):
        h, w = self._stdscr.getmaxyx()
        self._inputbuffer_window.resize(1, w)
        self._chatbuffer_window.resize(h - 2, w)
        self._inputbuffer_window.mvwin(h - 1, 0)
        self._render()

    def _render(self):
        h, w = self._stdscr.getmaxyx()
        self._stdscr.clear()
        self._stdscr.hline(h - 2, 0, "-", w)
        self._stdscr.refresh()
        self._render_chatbuffer()
        self._render_inputbuffer()

    def _render_chatbuffer(self):
        self._chatbuffer_window.clear()
        h, w = self._chatbuffer_window.getmaxyx()
        messages = list(self._chatbuffer)
        messages.reverse()
        messages = messages[:h-1]
        messages.reverse()
        if messages:
            author_maxlength = max([len(author) for author, _ in messages])
            i = 0
            for author, message in messages:
                if author:
                    line = "{0}: {1}".format(author.rjust(author_maxlength), message)
                else:
                    line = message
                if i >= h:
                    break
                self._chatbuffer_window.addstr(i, 0, line)
                i += 1
        self._chatbuffer_window.refresh()

    def _render_inputbuffer(self):
        self._inputbuffer_window.clear()
        h, w = self._inputbuffer_window.getmaxyx()
        self._inputbuffer_window.addstr(0, 0, self._inputbuffer)
        self._inputbuffer_window.refresh()

class ChatClient:
    def __init__(self, ui):
        self._ui = ui
        self._mqtt_client = None
        self._mqtt_host = None
        self._mqtt_port = None
        self._mqtt_transport = None
        self._nickname = "nobody"
        self._channel = None
    
    def run(self):
        self._print_appmessage("Hello!")
        self._print_appmessage("Change your nickname with /nickname")
        self._print_appmessage("Connect to a mqtt broker with /connect")
        self._print_appmessage("Join a channel with /join")

        while True:
            input = self._ui.readinput("{0}> ".format(self._nickname))
            if input in ["/q", "/quit"]:
                self._print_message(self._nickname, input)
                break
            elif input.startswith("/connect"):
                self._print_message(self._nickname, input)
                m = re.match("^\/connect[ ]?([^ $]*)[ ]?(.*)[ ]?([\d]*)$", input)
                if not m:
                    self._print_appmessage("Usage: /connect HOST TRANSPORT PORT")
                    continue

                host = m.group(1)
                if not host:
                    host = None
                if not m.group(2):
                    transport = None
                else:
                    transport = m.group(2)
                if not m.group(3):
                    port = None
                else:
                    port = int(m.group(3))
                
                self._connect(host, port, transport)
            elif input in ["/disconnect"]:
                self._print_message(self._nickname, input)
                self._disconnect()
            elif input.startswith("/join"):
                self._print_message(self._nickname, input)
                channel = input[len("/join "):].strip()
                self._join_channel(channel)
            elif input in ["/leave"]:
                self._print_message(self._nickname, input)
                self._leave_channel()
            elif input.startswith("/nickname"):
                self._print_message(self._nickname, input)
                nickname = input[len("/nickname "):]
                self._change_nickname(nickname)
            elif input in ["/h", "/help"]:
                self._print_message(self._nickname, input)
                self._print_appmessage("/h /help")
                self._print_appmessage("    print this help message")
                self._print_appmessage("/connect HOST PORT [tcp|websockets]")
                self._print_appmessage("    connect to an mqtt broker, the port and transport is optional")
                self._print_appmessage("/disconnect")
                self._print_appmessage("    disconnect from the mqtt broker")
                self._print_appmessage("/join CHANNEL")
                self._print_appmessage("    join a channel")
                self._print_appmessage("/leave")
                self._print_appmessage("    leave channel")
                self._print_appmessage("/nickname NAME")
                self._print_appmessage("    change your nickname")
                self._print_appmessage("/q /quit")
                self._print_appmessage("    leave the chat and close the application")
            else:
                # publish message
                self._send_message(input)

    def _on_connect(self, client, userdata, flags, rc):
        self._print_appmessage("Connected to {0}:{1}".format(self._mqtt_host, self._mqtt_port))
        if self._channel:
            # join again
            self._join_channel(self._channel)

    def _on_disconnect(self, client, userdata, rc):
        self._print_appmessage("Disconnected from {0}:{1}".format(self._mqtt_host, self._mqtt_port))

    def _on_message(self, client, userdata, msg):
        try:
            if self._channel is not None:
                if msg.topic == self._get_channel_topic():
                    payload = msg.payload.decode('utf8')
                    author, message = json.loads(payload)
                    self._print_message(author, message)
            else:
                print("Received an unhandled message on topic {0}".format(msg.topic))
        except Exception as ex:
            print("An unhandled exception occurred while processing a message on topic {0}: {1}".format(msg.topic, ex))
    
    def _connect(self, host=None, port=None, transport=None):
        if self._mqtt_client is not None:
            self._print_appmessage("Cannot connect: already connected")
            return
        transports_allowed = ['tcp', 'websockets']
        if transport is None:
            transport = transports_allowed[0]
        if transport not in transports_allowed:
            self._print_appmessage("Cannot connect: transport must be one of {0}".format(transports_allowed))
            return
        if port is None:
            if transport == "tcp":
                port = 1883
            elif transport == "websockets":
                port = 8000
        self._print_appmessage("Connecting to {0}:{1} using {2} ..".format(host, port, transport))
        mqtt_client = mqtt.Client(transport=transport)
        mqtt_client.on_connect = self._on_connect
        mqtt_client.on_message = self._on_message
        mqtt_client.on_disconnect = self._on_disconnect
        try:
            mqtt_client.connect(host, port)
        except Exception as ex:
            self._print_appmessage("Could not connect: {0}".format(str(ex)))
            return
        # this only runs if we can connect to the broker
        self._mqtt_client = mqtt_client
        self._mqtt_client.loop_start()
        self._mqtt_host = host
        self._mqtt_port = port
        self._mqtt_transport = transport
    
    def _disconnect(self):
        if self._mqtt_client is None:
            self._print_appmessage("Cannot disconnect: not connected yet")
            return
        
        self._mqtt_client.disconnect()
        self._mqtt_client.loop_stop()
        self._mqtt_client = None

    def _join_channel(self, channel):
        if not channel:
            self._print_appmessage("Channel name cannot be empty")
            return
        if channel.strip() != channel:
            self._print_appmessage("Channel name must not contain leading or trailing whitespaces")
            return
        if self._channel:
            # leave first
            self._leave_channel()
        self._channel = channel
        self._mqtt_client.subscribe(self._get_channel_topic())
        self._send_channel_message("{0} joined the channel".format(self._nickname))
    
    def _leave_channel(self):
        if not self._channel:
            self._print_appmessage("Cannot leave channel: join a channel first")
        
        self._mqtt_client.unsubscribe(self._get_channel_topic())
        self._send_channel_message("{0} left the channel".format(self._nickname))
        self._channel = None

    def _send_message(self, message):
        self._send_message_as(self._nickname, message)

    def _send_channel_message(self, message):
        self._send_message_as("", message)

    def _send_message_as(self, author, message):
        if self._mqtt_client is None:
            self._print_appmessage("Cannot send message: not connected yet")
            return
        if self._channel is None:
            self._print_appmessage("Cannot send message: join a channel first")
            return
        payload = json.dumps((author, message)).encode('utf8')
        self._mqtt_client.publish(self._get_channel_topic(), payload)
    
    def _change_nickname(self, nickname):
        if not nickname:
            self._print_appmessage("Nickname cannot be empty")
            return
        if nickname.strip() != nickname:
            self._print_appmessage("Nickname must not contain leading or trailing whitespaces")
            return
        if self._mqtt_client is not None and self._channel is not None:
            self._send_channel_message("{0} changed nickname to {1}".format(self._nickname, nickname))
        else:
            self._print_appmessage("Your nickname is now {0}".format(nickname))
        self._nickname = nickname
    
    def _get_channel_topic(self):
        return "chat/channel/{0}/message".format(self._channel)

    def _print_message(self, author, message):
        self._ui.chatbuffer_addmessage(author, message)
    
    def _print_appmessage(self, message):
        self._ui.chatbuffer_addmessage("$", message)

def main(stdscr):
    stdscr.clear()
    ui = ChatUI(stdscr)
    chatclient = ChatClient(ui)
    chatclient.run()

curses.wrapper(main)
