
echo "create mssql $mssqldb db........."
sqlcmd -s localhost -U sa -P $mssqlpassword -Q"create database $mssqldb;"

echo "establish postgres connection......."
pgcontainer=$(docker ps --filter expose=5432 --format {{.Names}})
docker exec $pgcontainer psql -U postgres -d $postgresqldb -c "\dt"

echo "script completed successfully........"