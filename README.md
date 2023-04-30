## Publish

dotnet publish -c Debug --runtime linux-arm --self-contained

## Deploy

scp -r .\Garage.Door.Opener\bin\Debug\net7.0\linux-arm\publish\* pi@garage-pi.local:/home/pi/myapp

## Linux

https://chronicdev.io/devlog/raspberry-pi-web-app-asp-dotnet-core

sudo systemctl stop kestrel-myapp.service

sudo cp -R /home/pi/myapp/* /var/www/myapp/

sudo systemctl start kestrel-myapp.service

systemctl status kestrel-myapp.service

### Service Setup

sudo nano /etc/systemd/system/kestrel-myapp.service

sudo journalctl -fu kestrel-myapp.service
sudo chmod +x /var/www/myapp/

## Home Assistant

You will need to setup a MQTT broker to send messages for Home Assistant to read. Mosquitto broker is one that works. The message to subscribe to in Home Assistant is `test`.