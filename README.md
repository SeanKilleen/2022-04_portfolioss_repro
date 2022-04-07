# PortfoliOSS Issue Reproduction

## Download DB Backup file

https://1drv.ms/u/s!AuaqsrDoMOUHobUtbEyYe2iDCnVHRQ?e=UKb2Mf

## Add Containers

SQL DB for persistence (modify the volume to point to the data file directory where you're storing the backup -- in the below example, `C:\DockerData\portfolioss`):

```cmd
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=yourStrong(!)Password" --name portfolioss -p 1433:1433 -v C:\DockerData\portfolioss:/mnt/portfolioss_backup -d mcr.microsoft.com/mssql/server:2019-latest
```

Seq for logging:

```cmd
docker run -p:5342:80 -p 5341:5341 -e "ACCEPT_EULA=Y" --name seq -d datalust/seq:latest
```

## Create & Restore DB

* Connect to the DB
* Execute the below scripts

```sql
CREATE DATABASE portfolioss
```

```sql
USE [master]
RESTORE DATABASE [portfolioss] FROM  DISK = N'/mnt/portfolioss_backup/master-202246-11-21-27.bak' WITH REPLACE, NOUNLOAD,  STATS = 5
```

## Run the app

* It'll take a little bit, since it will attempt to replay all the evetns

## Expected behavior

* All 1.4m events will be replayed to the actor
* All sequences events for the event types in question will be replayed

## Actual behavior

* Some of the events, particularly after 1.2ish million, will be missing
* Once you reach the last event, you can take the log message about the event #, and then run the following query to see some cases in which items are missing (which you can cross-reference agasint the logs to verify they were never received):

```sql
DECLARE @LatestItem int

SET @LatestItem = 1266150

select PersistenceId, max(SequenceNr) as MaxSequence, max (Ordering) as MaxOrdering from EventJournal where Ordering > @LatestItem and LEFT(PersistenceId, 4) = 'repo' group by PersistenceId order by PersistenceId
```
