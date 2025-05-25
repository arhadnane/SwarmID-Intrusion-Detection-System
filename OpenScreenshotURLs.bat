@echo off
echo.
echo 📸 Ouverture des URLs pour les captures d'écran SwarmID
echo ======================================================
echo.

echo 🚀 Ouverture du Dashboard SwarmID...
start http://localhost:5121
timeout /t 2 /nobreak >nul

echo 🚀 Ouverture de la documentation API...
start http://localhost:5112/swagger
timeout /t 2 /nobreak >nul

echo.
echo ✅ URLs ouvertes dans le navigateur
echo.
echo 📋 Captures d'écran à prendre :
echo    1. dashboard-main.png      (Page principale)
echo    2. traffic-analysis.png    (Analyse de trafic)
echo    3. anomaly-management.png  (Gestion anomalies)
echo    4. api-swagger.png         (Documentation API)
echo    5. realtime-monitoring.png (Monitoring temps réel)
echo    6. pcap-upload.png         (Upload PCAP)
echo.
echo 💡 Utilisez Windows + Shift + S pour capturer
echo 💾 Sauvegardez dans docs\images\ avec les noms exacts
echo.
echo 📖 Guide détaillé : docs\SCREENSHOT_GUIDE.md
echo.
pause
