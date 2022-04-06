# PortfoliOSS Reproduction

## 1: Container

SQL DB for persistence (modify the volume to point to the data file directory):

```cmd
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=yourStrong(!)Password" --name portfolioss -p 1433:1433 -v C:\DockerData\portfolioss:/mnt/portfolioss_backup -d mcr.microsoft.com/mssql/server:2019-latest
```

Seq for logging:

```cmd
docker run -p:5342:80 -p 5341:5341 -e "ACCEPT_EULA=Y" --name seq -d datalust/seq:latest
```

## 2: Create & Restore DB

* Connect to the DB
* Execute the below scripts

```sql
CREATE DATABASE portfolioss
```

```sql
USE [master]
RESTORE DATABASE [portfolioss] FROM  DISK = N'/mnt/portfolioss_backup/master-202246-11-21-27.bak' WITH REPLACE, NOUNLOAD,  STATS = 5
```

## 3: Run the app