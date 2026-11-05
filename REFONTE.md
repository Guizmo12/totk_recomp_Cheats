# Recette de la refonte — Hyrule Companion 0.2.0

Refonte de l’application WPF existante, réalisée à partir des références Item browser et Cheats du pack fourni. Les quatre onglets conservent leurs contrôles natifs et leurs actions. Les images servent au décor et aux illustrations, jamais de remplacement de l’interface.

## Vérifications réalisées

- Publication Release Windows .NET 8 réussie.
- Les 20 tests existants passent : filtrage, recherche, raccourcis, validation des pointeurs, refus des écritures invalides, restauration de vitesse, flèches et lots, rappel natif et conservation du contexte processeur.
- Catalogue : 1 337 entrées avec leurs images réelles, 10 catégories, lots de 5 et 10 flèches.
- Contrôles de l’interface : recherche, filtres, favoris avec restauration, pagination, sélection et identifiant d’acteur, disponibilité réelle de l’invocation, navigation, raccourcis affichés, lieux conservés, filtre des cheats actifs et état vide.
- Connexion réelle au jeu confirmée en lecture seule, tous les cheats désactivés.
- Captures des quatre pages à 1440×920, 1600×900, 1920×1080 et 1120×720 ; vérification du défilement des réglages, des cheats et du panneau d’invocation.
- Rendu natif WPF à 125 % (1280×720 points, 1600×900 pixels). Il s’agit d’une vérification de mise en page et de rastérisation, sans changer le réglage DPI global de Windows.
- Les six fichiers du moteur sont inchangés, vérifiés par SHA-256 : GameMemory, FrameBridge, PlayerLocator, JumpInput, Hotkeys, SpawnCatalog.

Les contrôles de logique utilisent une mémoire de test. Les invocations, téléportations et écritures de cheats en jeu n’ont pas été relancées pendant cette refonte graphique. Les comptes rendus de gameplay précédents restent distincts. Le saut reste expérimental et No fall damage reste indisponible sur cette version du jeu.

## Système graphique

`Themes/Tokens.xaml` définit la palette, les dégradés, les illustrations et les polices système. `Themes/Ornaments.xaml` reprend les symboles SVG du pack en dessins vectoriels WPF. `Themes/Controls.xaml` définit les contrôles, états de survol, focus, sélection et désactivation. `Presentation/Chrome.cs` fournit les cadres, le sceau Sheikah, les transitions courtes et le cache des vignettes. `Presentation/MainWindow.Presentation.cs` gère l’adaptation de la mise en page et la recette visuelle.

Les images sont décodées à la taille utile et mises en cache avec une limite de 256 vignettes. Les motifs sont vectoriels et statiques ; il n’y a pas de boucle d’animation permanente. Les animations discrètes respectent le réglage Windows et l’option Reduce interface animations dans Settings. Les ressources graphiques sont embarquées dans l’assembly ; le catalogue reste dans assets.

## Données personnelles

Favoris, lieux, raccourcis, chemin du jeu et préférences existantes conservés. L’historique de sélection utilisé pendant les vérifications est restauré. Le dossier backups et les sauvegardes du jeu n’ont pas été modifiés. Les archives de distribution excluent user-settings.json, les sauvegardes et les rapports contenant des détails du processus.

## Consultation

Les captures et le détail des assertions figurent dans `visual-review/`. Pour relancer les vérifications sans écrire dans le jeu : `HyruleCompanion.exe --visual-review --connect`. Fermer préalablement l’application. Ne pas combiner cette option avec les vérifications de gameplay.
