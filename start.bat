@echo off
echo Starting MarketCore...
cd /d "%~dp0market-core-client"
start "Angular" cmd /k "npm start"
echo Angular started at http://localhost:4200
