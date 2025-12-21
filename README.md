# Gauniv - Plateforme de Distribution de Jeux

## Répartition du travail

**Binôme:**
- Interface web d'administration: Développée par mon binôme
- Application cliente Windows: Développée par moi
- API REST: Collaboration (backend par binôme, intégration par moi)

---

## Application Windows (MAUI)

### Fonctionnalités implémentées

#### 1. Navigation et Architecture
- Application MAUI avec architecture MVVM utilisant CommunityToolkit.Mvvm
- Navigation par Shell avec menu flyout
- Trois pages principales: Store, Profile, Library
- Thème sombre inspiré de Steam pour une meilleure expérience utilisateur

#### 2. Page Store (Index)
**Affichage des jeux:**
- Liste de tous les jeux disponibles sur la plateforme
- Pagination: chargement de 200 jeux depuis l'API
- Cartes de jeux avec informations: nom, description, prix, catégories
- Indication visuelle si le jeu est déjà possédé

**Système de filtrage:**
- Recherche par nom avec SearchBar
- Filtre par catégorie via Picker
- Filtre par prix: champs Min et Max
- Cases à cocher: "Owned Games" et "Not Owned"
- Bouton "Clear Filters" pour réinitialiser
- Filtres appliqués en temps réel côté client
- Tri alphabétique des résultats

**Actions:**
- Achat de jeux via API
- Navigation vers les détails du jeu
- Rechargement automatique lors du retour sur la page

#### 3. Page Library (MyGames)
**Gestion de la bibliothèque:**
- Liste des jeux possédés par l'utilisateur
- Statut de chaque jeu: Downloaded, Not Downloaded, Running
- Taille du jeu affichée

**Système de filtrage:**
- Recherche par nom
- Filtre par catégorie
- Cases à cocher: Downloaded, Not Downloaded, Running
- Bouton "Clear Filters"

**Actions disponibles:**
- Téléchargement: bouton visible uniquement si non téléchargé
- Lancement: bouton visible uniquement si téléchargé et non en cours
- Arrêt: bouton visible uniquement si le jeu est en cours d'exécution
- Suppression: bouton visible uniquement si téléchargé
- Rafraîchissement par pull-to-refresh

**Gestion des binaires:**
- Téléchargement des jeux depuis l'API
- Stockage local via LocalGameManager
- Lancement et arrêt des jeux
- Suppression des fichiers locaux
- Mise à jour automatique des statuts

#### 4. Page Profile
**Authentification:**
- Formulaire de connexion (email/password)
- Affichage de l'email connecté
- Bouton de déconnexion
- Gestion du token d'authentification

**Restriction admin:**
- Détection des tentatives de connexion admin
- Blocage côté serveur avec code HTTP 403
- Message d'erreur explicite dirigeant vers l'interface web
- Avertissement visible sur la page de profil

#### 5. Page Game Details
**Affichage des informations:**
- Nom du jeu
- Description avec support HTML formaté
- Prix
- Catégories
- Statut de possession

**Support HTML dans les descriptions:**
- Détection automatique du contenu HTML
- Rendu via WebView avec style Steam intégré
- Fallback texte simple pour descriptions non-HTML
- Styles prédéfinis: titres, listes, texte en gras, couleurs

**Actions:**
- Achat du jeu si non possédé
- Bouton retour

#### 6. Services et Communication API
**GameService:**
- Authentification Bearer token
- Récupération de la liste des jeux avec pagination
- Récupération des jeux possédés
- Récupération des catégories
- Téléchargement des binaires de jeux
- Achat de jeux
- Gestion des erreurs HTTP

**LocalGameManager:**
- Singleton pour la gestion centralisée
- Enregistrement des jeux téléchargés
- Vérification du statut de téléchargement
- Lancement de jeux (simulation)
- Arrêt de jeux
- Suppression de fichiers
- Gestion des chemins de téléchargement

#### 7. Converters XAML
- BoolToTextConverter: conversion booléen vers texte
- BoolToColorConverter: couleurs conditionnelles
- InvertedBoolConverter: inversion de booléens
- IsNotNullConverter: vérification de nullité
- ListToStringConverter: catégories en chaîne
- StripHtmlConverter: suppression des balises HTML
- IsHtmlConverter: détection de contenu HTML
- HtmlToWebViewSourceConverter: rendu HTML avec style Steam

### Respect des exigences

**Lister les jeux:**
- Pagination implémentée (200 jeux)
- Filtres multiples fonctionnels
- Scroll dans la liste

**Lister les jeux possédés:**
- Pagination implémentée (50 jeux)
- Filtres par catégorie et statut
- Pull-to-refresh

**Détails d'un jeu:**
- Toutes les informations affichées
- Statuts visuels clairs

**Télécharger, supprimer, lancer:**
- Boutons conditionnels selon le statut
- Gestion complète du cycle de vie

**Visualiser l'état:**
- Indicateurs visuels: Running, Installed, Not Installed
- Mise à jour en temps réel des statuts

**Profil:**
- Login/Logout fonctionnel
- Affichage des identifiants
- Persistance de la session

**Données depuis le serveur:**
- Toutes les données proviennent des APIs REST
- Aucune donnée statique

### Option implémentée

**Description avec formatage HTML:**
- Support complet du HTML dans les descriptions
- Rendu avec WebView et style personnalisé
- Balises supportées: titres, paragraphes, listes, gras, italique, liens
- Classes CSS prédéfinies: highlight, warning, success, error
- Thème Steam intégré: fond sombre, texte clair, accents colorés
- Backward compatible: descriptions texte simple fonctionnent toujours
- Documentation fournie dans HTML_DESCRIPTION_FORMAT.md

---

## Contributions au Serveur Web

### API REST
**Endpoints utilisés par l'application:**
- GET /api/1.0.0/Games/List: liste avec filtres et pagination
- GET /api/1.0.0/Games/MyGames: jeux possédés avec pagination
- GET /api/1.0.0/Games/Categories: toutes les catégories
- GET /api/1.0.0/Games/Details/{id}: détails d'un jeu
- GET /api/1.0.0/Games/Download/{id}: téléchargement binaire
- POST /api/1.0.0/Games/Purchase/{id}: achat de jeu
- POST /Bearer/login: authentification

### Restriction admin
**Sécurité ajoutée:**
- Modification du endpoint /Bearer/login
- Vérification du rôle utilisateur avant authentification
- Retour HTTP 403 Forbidden pour les comptes admin
- Message explicite pour rediriger vers l'interface web
- Protection contre l'utilisation de comptes admin sur le client

### Tests et validation
- Compilation sans erreur du client MAUI
- Tests de tous les endpoints API
- Validation du flux complet: connexion, achat, téléchargement, lancement
- Tests des filtres et pagination
- Validation du rendu HTML

---

## Technologies utilisées

**Client Windows:**
- .NET MAUI 10.0
- CommunityToolkit.Mvvm pour MVVM
- ObservableObject et ObservableProperty pour le binding réactif
- WebView pour le rendu HTML
- HttpClient pour les appels API

**Serveur:**
- ASP.NET Core
- PostgreSQL avec Entity Framework
- ASP.NET Identity pour l'authentification
- Bearer token JWT

**Communication:**
- API REST avec JSON
- Streaming de fichiers pour les binaires
- Bearer token authentication

---

## Points forts de l'implémentation

1. **Architecture propre:** MVVM strict avec séparation des responsabilités
2. **UX soignée:** Thème Steam cohérent, boutons conditionnels, feedback utilisateur
3. **Performance:** Filtres côté client, pagination, streaming de fichiers
4. **Sécurité:** Validation des tokens, restriction admin, vérification ownership
5. **Extensibilité:** Converters réutilisables, services modulaires
6. **Options:** Support HTML dans descriptions (option demandée)
7. **Robustesse:** Gestion d'erreurs, messages explicites, fallbacks

---

## Comptes de test

**Utilisateur standard:**
- Email: test@test.com
- Mot de passe: password
- Accès: Application cliente + Interface web

**Administrateur:**
- Email: admin@gauniv.com
- Mot de passe: Admin123
- Accès: Interface web uniquement (bloqué sur client)

---

## Remarques

L'application cliente Windows répond à toutes les exigences fonctionnelles demandées pour la partie "Application (MAUI)". L'option "Description avec formatage" a été implémentée avec support HTML complet.

La partie serveur de jeu multijoueur (Gauniv.GameServer) et le jeu (Gauniv.Game) n'ont pas été développés dans le cadre de ce projet.
