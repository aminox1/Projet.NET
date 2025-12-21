# Gauniv - Plateforme de distribution de jeux

Ce dépôt contient la solution Gauniv : un serveur web (API + interface d'administration et d'achat de jeux) et une application cliente Windows (MAUI) permettant de parcourir, acheter, télécharger et lancer des jeux.

---

## Table des matières

- [Etat & répartition](#etat--r%C3%A9partition)
- [Présentation rapide](#pr%C3%A9sentation-rapide)
- [Application cliente Windows (MAUI) - résumé des fonctionnalités](#application-cliente-windows-maui)
  - [Navigation & architecture](#navigation--architecture)
  - [Store (Index)](#store-index)
  - [Library (Mes jeux)](#library-mes-jeux)
  - [Profile](#profile)
  - [Game Details](#game-details)
  - [Services & gestion locale](#services--gestion-locale)
- [Serveur Web (API)](#serveur-web-api)
  - [Endpoints utilisés par le client](#endpoints-utilis%C3%A9s-par-le-client)
  - [Sécurité / restriction admin](#s%C3%A9curit%C3%A9--restriction-admin)
- [Serveur Web - Interface d'administration et d'achat de jeux](#serveur-web--interface-dadministration-hors-api)
  - [Administration](#administration)
  - [Utilisateur (administrateur et joueurs)](#utilisateur-vue-serveur-web)
  - [Tout le monde (accès public)](#tout-le-monde-acc%C3%A8s-public)
  - [Options (bonus)](#options-et-contrainte-bonus--bonnes-pratiques)
- [Technologies](#technologies)
- [Comptes de test](#comptes-de-test)
- [How to run (exécution rapide)](#how-to-run-ex%C3%A9cution-rapide)
- [Points forts & notes](#points-forts--notes)

---

## Etat & répartition

- Statut : application web et client lourd développés.
- Répartition (conforme au dépôt) :
  - Application web d'administration, de consultation et d'achat de jeux : développée par Mamitiana (interface web / backend).
  - Application cliente Windows (MAUI) : développée par Amine.
  - API REST : collaboration (backend par Mamitiana, intégration côté client par Amine).

---

## Présentation rapide

Gauniv propose :

- Une interface d'administration web pour gérer jeux & catégories avec la possibilités d'acheter des jeux pour les joueurs.
- Une API REST destinée au client Windows (et à d'autres clients).
- Une application cliente Windows (MAUI) avec thème sombre inspiré de Steam, prise en charge du téléchargement et du lancement de jeux, et rendu HTML pour les descriptions.

---

## Application cliente Windows (MAUI)

Résumé des fonctionnalités implémentées côté client (détaillé fourni par l'équipe) :

### Navigation & architecture

- Application MAUI en MVVM (CommunityToolkit.Mvvm).
- Navigation par Shell avec menu (Flyout).
- Pages principales : Store (Store/Index), Library (Mes jeux), Profile.
- Thème sombre inspiré de Steam (UX cohérente : fond sombre, accents colorés).

### Store (Index)

- Affichage : liste de tous les jeux fournis par l'API.
- Pagination : prise en charge (chargement de 200 jeux depuis l'API).
- Cartes de jeux : nom, description, prix, catégories, indication visuelle si jeu possédé.
- Filtres : recherche par nom (SearchBar), filtre par catégorie (Picker), filtre prix (min/max), cases "Owned" / "Not Owned".
- Bouton "Clear Filters" pour réinitialiser.
- Filtres appliqués côté client en temps réel ; tri alphabétique disponible.
- Actions : achat via API, navigation vers détails, rechargement automatique au retour.

### Library (Mes jeux)

- Liste des jeux possédés avec statut : Downloaded, Not Downloaded, Running.
- Taille affichée, filtres (nom, catégorie, statut : Downloaded/Not Downloaded/Running).
- Actions conditionnelles : télécharger, lancer, arrêter, supprimer (boutons visibles selon statut).
- Pull-to-refresh et pagination (ex : 50 jeux).
- Gestion des binaires : téléchargement streaming depuis l'API, stockage local via LocalGameManager, gestion du cycle de vie (télécharger/lancer/supprimer), mise à jour automatique des statuts.

### Profile

- Authentification : formulaire email/password.
- Affichage de l'email connecté, bouton de déconnexion.
- Gestion du token Bearer (persisté selon configuration).
- Restriction admin : détection des tentatives de connexion admin côté client et blocage côté serveur (HTTP 403) -message explicite et directive vers l'interface web.

### Game Details

- Affiche : nom, description (support HTML), prix, catégories, statut de possession.
- Rendu HTML : WebView stylée (thème Steam) pour les descriptions HTML, fallback en texte simple.
- Actions : achat si non possédé, bouton retour.

### Services & gestion locale

- `GameService` : authentification Bearer, récupération lists/pagination, achats, téléchargement des binaires, gestion des erreurs HTTP.
- `LocalGameManager` : singleton gérant les jeux locaux (chemins, statuts, lancement/arrêt/suppression).
- Converters XAML utiles fournis : BoolToText, BoolToColor, IsHtml, StripHtml, ListToString, etc.

---

## Serveur Web (API)

Le serveur (ASP.NET Core) expose les endpoints utilisés par le client :

### Endpoints principaux utilisés

- `GET /api/1.0.0/Games/List` - liste paginée avec filtres (nom, catégories, prix, possédé, taille).
- `GET /api/1.0.0/Games/MyGames` - jeux possédés (auth requis), pagination.
- `GET /api/1.0.0/Games/Categories` - toutes les catégories.
- `GET /api/1.0.0/Games/Details/{id}` - détails d’un jeu.
- `GET /api/1.0.0/Games/Download/{id}` - téléchargement du binaire (streaming, ne charge pas tout en mémoire).
- `POST /api/1.0.0/Games/Purchase/{id}` - achat d’un jeu.
- `POST /Bearer/login` -authentification (renvoie token Bearer).

### Sécurité & restriction admin

- La logique d'authentification a été adaptée pour empêcher l'utilisation de comptes admin depuis le client MAUI : tentative de login admin retourne HTTP 403 et un message invitant à utiliser l'interface web d'administration.

---

## Serveur Web

La partie serveur web fournit, en complément de l'API REST, une interface web accessible via le navigateur (pages Razor / MVC) destinée à la fois aux administrateurs et aux joueurs.

- Pour les administrateurs : une interface d'administration complète (gestion des jeux, catégories, uploads, logs d'audit, etc.).
- Pour les joueurs : un catalogue consultable depuis le navigateur (liste des jeux, filtres, détails, catégories) et la possibilité d'acheter des jeux via l'interface web.

Ci‑dessous les fonctionnalités intégrées côté serveur web (hors API) :

### Administration

Un administrateur peut :

- Ajouter des jeux
  - Formulaire de création avec upload du binaire (streamé vers le stockage), sélection des catégories et métadonnées (nom, description, prix, taille estimée).
- Supprimer des jeux
- Modifier un jeu
  - Édition des métadonnées, ajout/suppression de catégories, remplacement du binaire (avec streaming et rotation du nom de fichier).
- Ajouter de nouvelles catégories
  - Création rapide via formulaire (nom, description, image optionnelle).
- Modifier une catégorie
  - Édition du nom, de la description et des métadonnées associées.
- Supprimer une catégorie

Toutes les actions d'administration sont restreintes par la politique d'authentification (rôle `Admin`) et enregistrent des logs d'audit (qui a fait quoi et quand).

### Joueur (vue serveur web)

Côté interface web, un utilisateur connecté peut :

- Consulter la liste des jeux possédés (interface "Mes jeux" ou tableau de bord personnel).
- Acheter un nouveau jeu depuis l'interface (procédure d'achat via formulaire / bouton et intégration du paiement simulé / marqueur d'achat en base).
- Voir la liste complète de ses jeux et leur état (téléchargé, disponible, en cours d'utilisation).
- Consulter la liste des autres joueurs inscrits et leurs statuts en temps réel (présence) - affichage via SignalR pour actualisation instantanée des statuts.

### Tout le monde (accès public)

Les pages publiques permettent :

- Consulter la liste de tous les jeux disponibles (catalogue public).
- Filtrer le catalogue par :
  - nom (recherche)
  - prix (min/max)
  - catégorie (multi-sélection)
  - possédé (dans le cas d'une vue utilisateur) - filtre côté serveur pour les vues personnalisées
  - taille (bornes en Mo)
- Consulter la liste de toutes les catégories avec leurs descriptions et visuels.

Les listes publiques supportent pagination, tri et/ou filtres côté serveur afin d'optimiser la charge et permettre des requêtes efficaces.

### Options

- Afficher des filtres directement dans l'interface des jeux (catégorie / prix / possédé) pour une expérience admin/éditeur plus fluide.
- Stockage des binaires hors de la base de données :
  - Les binaires (jeux ZIP, payloads) sont stockés sur le disque du serveur (`wwwroot/uploads` par défaut), avec un chemin ou URL conservé en base.
  - Lors de l'upload, les fichiers sont streamés directement vers le stockage (pas de chargement complet en mémoire) pour supporter des fichiers de plusieurs Gio.
- Téléchargement / distribution :
  - Le serveur stream les fichiers aux clients (utilisation d'un FileStreamResult / Stream) - évite d'allouer la mémoire pour tout le binaire.

---

## Technologies & outils

- Client Windows : .NET MAUI (10.0), CommunityToolkit.Mvvm, WebView, HttpClient.
- Serveur : ASP.NET Core, Entity Framework Core, PostgreSQL, ASP.NET Identity (JWT Bearer tokens).
- Communication : API REST JSON, streaming pour les binaires.

---

## Comptes de test

- Utilisateur standard (joueur) :
  - Email : `test@test.com`
  - Mot de passe : `password`
- Administrateur (interface web seulement) :
  - Email : `admin@gauniv.com`
  - Mot de passe : `admin123`

---

## How to run (exécution rapide)

1. Restaurer et builder la solution :

```bash
cd /Users/Clicar/Mamitiana/cours/Dotnet/projectGame/Projet.NET
dotnet restore
dotnet build Gauniv.sln
```

2. Lancer le serveur web :

```bash
cd Gauniv.WebServer
dotnet run
```

3. Lancer le client (MAUI/WPF selon configuration) :

```bash
cd Gauniv.Client
dotnet run
```

### Base de données PostgreSQL (Docker)

Le projet inclut un fichier Docker Compose pour démarrer une instance PostgreSQL locale. Pour lancer la base de données (en arrière-plan) depuis la racine du dépôt, exécute :

```bash
# Démarrer PostgreSQL via docker-compose (fichier fourni : docker-compose-db.yml)
docker-compose -f docker-compose-db.yml up -d
```

Une fois PostgreSQL démarré et accessible, exécute les migrations EF Core (depuis le dossier `Gauniv.WebServer`) pour créer/mettre à jour la base :

```bash
cd Gauniv.WebServer
# applique les migrations et crée la base (nécessite dotnet-ef)
dotnet ef database update
```

---

## Points forts

- Architecture MVVM propre côté client, séparation des responsabilités.
- UX soignée : thème sombre inspiré de Steam, feedbacks, boutons conditionnels.
- Performance : pagination, filtres côté client, streaming des binaires.
- Sécurité : tokens Bearer, restriction admin sur le client.

---

## Remarques

- La partie serveur de jeu (Gauniv.GameServer) et le jeu (Gauniv.Game) n'ont pas été développés dans ce dépôt.

---
