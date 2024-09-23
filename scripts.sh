
echo "running script for mssql db creation: $mssqldb"

sqlcmd -s localhost -U sa -P $mssqlpassword -Q"create database $mssqldb;"

echo "script completed successfully...."