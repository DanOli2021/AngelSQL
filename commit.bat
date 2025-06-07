@echo off
cd /d C:\AngelSQL\AngelNET\AngelSQLServer

git add .
git commit -m %1
git push
