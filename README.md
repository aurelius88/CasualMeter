# CasualMeter with output modification
This fork of [CasualMeter] modifies the output such that it is printed in multiple lines and tries to format the text in something more readable. I also deactivated the Update-function, so it won't accidently update to the original version. This also means updating has to be done manually.

### Formating

As you will see in the default settings file, there is a placeholder like `{Name,-20}`. This means the placeholder will get at least 20 fix chars aligned to the left (because of that `-`). All chars that are not used are filled by spaces.

For example the name would be `"Mr.X"`. The full string would be:`"Mr.X<- 16 spaces ->"`.

`{Name,20}` therefore would be replaced by `"<- 16 spaces ->Mr.X"`.

#### Placeholders

| Placeholder | Output | Example
| --- | --- | --- |
| FullName | Server-Name : Player-Name | Killian : A.Player |
| Name | Player-Name | A.Player |
| Class | Class-Name | Warrior |
| ClassAndName | (Class-Name) Player-Name | (Warrior) A.Player |
| NameAndClass | Player-Name (Class-Name) | A.Player (Warrior) |
| Crits | CritCount | 34 |
| Hits | HitCount | 68 |
| DamagePercent | Damage-in-percent% | 74% |
| CritPercent | Crit-in-percent% | 50% |
| Damage | Damage | 240k |
| DPS | DPS/s | 29k/s |
| DamageReceived | Received-Damage | 4k |
| RDPS | Received-DPS/s | 800/s |
| Heal | Healed | 0 |
| HealReceived | Received-Heal | 6k |
| HPS | HPS/s | 0/s |

Original text of CasualMeter:
----
# CasualMeter
[CasualMeter] is a free open-source DPS meter for TERA based off [TeraDamageMeter].  The majority of the new stuff is UI and UX improvements, but there are some bug fixes as well.  This [fork] is also worth mentioning since I borrowed the dps paste and settings storage from there.

### Features

Here are some of the features that are currently implemented:
* View damage dealt, damage healed, dps, total damage
* View detailed skill breakdown with three different views
* Will only show up while you are playing Tera
* Paste dps
* Review previous encounters
* Customizable hotkeys (through settings file)
* Updates automatically (checks when you start the application)
* Single instance (will not open up multiple meters if you launch by accident)

### Roadmap for major features
* TBD

### Third-Party libraries

* [MvvmLight] - MVVM Framework
* [Hardcodet] - WPF Taskbar Notification Icon
* [log4net] - Logging
* [Nicenis] - INotifyProperyChanged implementation
* [GlobalHotKey] - Global hotkeys
* [Newtonsoft] - Json serializer/deserializer
* [SharpPCap] - Wrapper around WinPCap, used for reading packets

### Installation
* Install WinPCap first: http://www.winpcap.org/install/default.htm
* http://lunyx.net/CasualMeter/Setup.exe
* If you see an issue with download on Chrome, right-click and choose Keep

### Usage

* https://github.com/lunyx/CasualMeter/wiki

### Development

You may contribute by submitting a pull request for bugfixes/updates between patches. A guide to updating opcodes (when it breaks after a patch) can be found [here]. In case it goes down, here's an [imgur mirror].  Pastebins: [1] and [2]

License
----

MIT



[//]: # (These are reference links used in the body of this note and get stripped out when the markdown processor does its job. There is no need to format nicely because it shouldn't be seen. Thanks SO - http://stackoverflow.com/questions/4823468/store-comments-in-markdown-syntax)

   [CasualMeter]: <https://github.com/lunyx/CasualMeter>
   [MvvmLight]: <http://www.mvvmlight.net/>
   [Hardcodet]: <http://www.hardcodet.net/wpf-notifyicon>
   [log4net]: <https://logging.apache.org/log4net/>
   [Nicenis]: <https://nicenis.codeplex.com/>
   [GlobalHotKey]: <https://github.com/kirmir/GlobalHotKey/>
   [Newtonsoft]: <http://www.newtonsoft.com/json>
   [TeraDamageMeter]: <https://github.com/gothos-folly/TeraDamageMeter>
   [fork]: <https://github.com/bonekid/TeraDamageMeter>
   [here]: <https://forum.ragezone.com/f797/release-tera-live-packet-sniffer-1052922/index2.html#post8369480>
   [imgur mirror]: <http://i.imgur.com/VTaWEe9.png>
   [1]: <http://pastebin.com/qTGzrW8w>
   [2]: <http://pastebin.com/BTu7mm5C>
   [SharpPCap]: <http://www.codeproject.com/Articles/12458/SharpPcap-A-Packet-Capture-Framework-for-NET>
