# pyIFCTools

## Todo
- write documentation and how to use on computer without conda env
- Re-generated Environment.yml before shipping

## Voraussetzungen
Um die entwickelten Tools nutzen zu können, muss eine entsprechende
Pythonlaufzeitumgebung auf dem Computer vorhanden sein.

Die Tools wurden mit der Python-Version 3.11 entwickelt. Weiterhin werden unter anderem 
folgende Bibliotheken verwendet: 
- IfcOpenShell
- PythonOCC
- Numpy
- PyQT5 (nur für Visualisierung notwendig)

Es wird empfohlen eine eigene virtuelle Python Umgebung mittels Miniconda zu erstellen
Miniconda kann [hier](https://docs.conda.io/en/latest/miniconda.html) heruntergeladen und installiert
werden. Nachdem Miniconda erfolgreich installiert wurde kann die notwendige Python-Umgebung
mit Hilfe der mitgeliferten Environment.yml Datei wiederhergestellt werden. 

Dazu muss zunächst die mit Miniconda mitgelieferte Anaconda Prompt gestartet und folgender
Befehl abgesetzt werden

```
conda env create -f environment.yml
```

Dies erstellt eine neue virtuelle Umgebung und installiert alle notwendigen Packages im 
Standard-Pfad von Miniconda. Wird ein anderer Installationsort gewünscht, kann dieser über
den Parameter `-p` angegeben werden:

```
conda env create -f environment.yml -p D:\dev\envs\env_name
```

