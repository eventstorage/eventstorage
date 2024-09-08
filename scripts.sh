
echo "running script for mssql db creation: $mssql_db"

sqlcmd -s localhost -U sa -P $mssqlpassword -q "create database $mssqldb;"

echo "script completed successfully...."