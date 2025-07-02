# deploy.ps1

cd "D:\Developers Hub\MpesaIntegration"
git add .

$commitMsg = Read-Host "Enter commit message"
git commit -m "$commitMsg"
git push origin main

Write-Host "`n✅ Code pushed to GitHub. Render will auto-deploy..."

# 🌐 Automatically open the deployed Render app in the browser
Start-Process "https://mpesa-integration-nbyo.onrender.com"
