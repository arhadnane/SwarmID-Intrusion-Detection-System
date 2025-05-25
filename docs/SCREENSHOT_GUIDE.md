# Guide pour Prendre les Captures d'Écran SwarmID

Ce guide vous explique comment prendre et ajouter les captures d'écran pour documenter le système SwarmID.

## 🎯 Captures d'Écran Requises

### 1. **dashboard-main.png** - Tableau de Bord Principal
**URL**: http://localhost:5121
**Contenu à capturer**:
- Page d'accueil avec les statistiques en temps réel
- Métriques de performance des algorithmes
- Nombre d'anomalies détectées
- État du système (services actifs)

**Instructions**:
1. Ouvrez http://localhost:5121 dans votre navigateur
2. Attendez que toutes les données se chargent
3. Prenez une capture d'écran de la page complète
4. Sauvegardez comme `docs/images/dashboard-main.png`

### 2. **traffic-analysis.png** - Analyse du Trafic
**URL**: http://localhost:5121/traffic
**Contenu à capturer**:
- Interface d'upload de fichiers PCAP
- Sélecteur de type de données (Zeek, Snort, PCAP)
- Bouton "Analyze File"
- Section de monitoring en temps réel

**Instructions**:
1. Naviguez vers http://localhost:5121/traffic
2. Assurez-vous que l'interface d'upload est visible
3. Prenez une capture d'écran de la page entière
4. Sauvegardez comme `docs/images/traffic-analysis.png`

### 3. **anomaly-management.png** - Gestion des Anomalies
**URL**: http://localhost:5121/anomalies
**Contenu à capturer**:
- Liste des anomalies détectées
- Filtres et options de recherche
- Détails des anomalies avec timestamps
- Actions de gestion (statut, feedback)

**Instructions**:
1. Allez à http://localhost:5121/anomalies
2. Si possible, uploadez d'abord un fichier PCAP pour avoir des données
3. Prenez une capture d'écran avec quelques anomalies visibles
4. Sauvegardez comme `docs/images/anomaly-management.png`

### 4. **api-swagger.png** - Documentation API
**URL**: http://localhost:5112/swagger
**Contenu à capturer**:
- Interface Swagger complète
- Liste des endpoints API
- Modèles de données
- Boutons "Try it out"

**Instructions**:
1. Ouvrez http://localhost:5112/swagger
2. Dépliez quelques sections (Traffic, Anomalies)
3. Prenez une capture d'écran de l'interface Swagger
4. Sauvegardez comme `docs/images/api-swagger.png`

### 5. **realtime-monitoring.png** - Monitoring Temps Réel
**URL**: http://localhost:5121/traffic (section monitoring)
**Contenu à capturer**:
- Graphiques de performance en temps réel
- Statistiques live
- Indicateurs de statut des algorithmes
- Métriques de détection

**Instructions**:
1. Retournez à http://localhost:5121/traffic
2. Focalisez sur la section "Real-time Monitoring"
3. Attendez que les données se mettent à jour
4. Prenez une capture d'écran de cette section
5. Sauvegardez comme `docs/images/realtime-monitoring.png`

### 6. **pcap-upload.png** - Upload de Fichiers PCAP
**URL**: http://localhost:5121/traffic (section upload)
**Contenu à capturer**:
- Formulaire d'upload avec un fichier sélectionné
- Dropdown de type de données sur "PCAP File"
- Bouton "Analyze File" prêt à être cliqué
- Barre de progression si possible

**Instructions**:
1. Sur http://localhost:5121/traffic
2. Cliquez sur "Select traffic data file"
3. Sélectionnez un des fichiers PCAP (port-scan.pcap ou suspicious-traffic.pcap)
4. Changez le type de données vers "PCAP File"
5. Prenez une capture d'écran avant de cliquer "Analyze File"
6. Sauvegardez comme `docs/images/pcap-upload.png`

## 🛠️ Instructions Techniques

### Outils Recommandés
- **Windows**: Outil Capture d'écran (Win + Shift + S)
- **Alternative**: Snipping Tool ou Print Screen
- **Navigateur**: Mode développeur pour captures précises

### Paramètres de Capture
- **Format**: PNG (pour la qualité)
- **Résolution**: Pleine résolution (ne pas redimensionner)
- **Qualité**: Haute qualité pour la lisibilité

### Optimisation des Images
```powershell
# Si vous avez ImageMagick installé, vous pouvez optimiser les images :
magick convert "docs/images/dashboard-main.png" -quality 85 -resize 1200x "docs/images/dashboard-main.png"
```

## 📁 Structure des Fichiers

Après avoir pris toutes les captures d'écran, votre structure devrait ressembler à :

```
docs/
└── images/
    ├── dashboard-main.png
    ├── traffic-analysis.png
    ├── anomaly-management.png
    ├── api-swagger.png
    ├── realtime-monitoring.png
    └── pcap-upload.png
```

## ✅ Checklist de Validation

- [ ] dashboard-main.png (Page principale avec métriques)
- [ ] traffic-analysis.png (Interface d'analyse de trafic)
- [ ] anomaly-management.png (Gestion des anomalies)
- [ ] api-swagger.png (Documentation Swagger API)
- [ ] realtime-monitoring.png (Monitoring temps réel)
- [ ] pcap-upload.png (Upload de fichiers PCAP)

## 🎨 Conseils pour de Meilleures Captures

1. **Données de Test**: Uploadez d'abord les fichiers PCAP pour avoir des données à afficher
2. **Navigateur Propre**: Utilisez un navigateur sans extensions pour éviter les distractions
3. **Résolution**: Utilisez une résolution d'écran standard (1920x1080 recommandée)
4. **Timing**: Attendez que toutes les animations et chargements soient terminés
5. **Focus**: Mettez en évidence les fonctionnalités importantes dans chaque capture

---

Une fois toutes les captures d'écran prises et placées dans le dossier `docs/images/`, le README.md affichera automatiquement les images avec leurs descriptions.
