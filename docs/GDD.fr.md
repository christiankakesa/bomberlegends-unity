> ⚠️ **ARCHIVE — NE PAS MODIFIER.**
> Version française originale, conservée comme référence historique.
> Le document de travail est [`GDD.md`](GDD.md) (anglais). Toute modification se fait là-bas.

---

# **GAME DESIGN DOCUMENT (GDD)**

# **Titre du Projet : Bomber Legends**

**Version :** 1.1

**Date :** 23 Mars 2026 (v1.0) — en-tête révisé le 5 Août 2026

**Auteur :** Christian Kakesa

**Type de Jeu :** Action / Puzzle / Stratégie Tactique (Type "Bomberman" Modernisé)

**Plateforme Cible :** Mobile-first.

1. Android — cible principale dès le Milestone 0
2. WebGL — playtests distants et itch.io
3. iOS — soft launch
4. Desktop Windows / Linux / Mac — portage ultérieur, Steam plus tard

Consoles (PS5, Xbox, Switch 2) hors périmètre jusqu'à validation du prototype.

**Modèle Économique :** Free-to-Play (F2P)

**Moteur :** Unity 6.3 LTS ou supérieur. Décidé, non rediscuté.

**Briques techniques :**

* **Client :** Unity 6.3 LTS, C\#
* **Backend :** Nakama, PostgreSQL/CockroachDB, Go — **reporté au Milestone 9**, aucun développement backend avant validation du prototype

**Décisions techniques :** voir `docs/01-ANALYSIS.md` §13 et `docs/README.md`.

---

## **1\. RÉSUMÉ EXÉCUTIF (PITCH)**

---

## **2\. PILIERS DE GAMEPLAY (GAMEPLAY PILLARS)**

1. **Placement Tactique de Bombes :** Le cœur du gameplay classique. Les joueurs doivent planifier leurs chaînes d'explosion pour nettoyer le terrain et piéger les ennemis sans se faire piéger eux-mêmes.  
2. **Afro-Futurisme Néon :** Une esthétique visuelle unique, mélangeant architecture technologique et éléments culturels africains, pour une ambiance unique et immersive.  
3. **Gestion des Compétences (Actives/Passives) :** Contrairement au "Bomberman" classique, le joueur gère un arbre de compétences actives et passives qui changent l'approche tactique, plutôt que de simples power-ups aléatoires.  
4. **Action Contre-la-Montre :** La pression du temps est un ennemi constant, forçant le joueur à prendre des risques et à optimiser ses mouvements.

---

## **3\. L'UNIVERS ET L'ESTHÉTIQUE**

### **3.1. Le Contexte (Lore)**

Dans la mégapole d'Ébène-Prime, la corporation "Sombra-Corps" contrôle tout le flux de données. Le joueur est un membre de la "Génération Néon", des rebelles qui piratent physiquement les serveurs de la ville pour libérer l'information. Chaque niveau est un "secteur" (un quartier) que le joueur doit traverser pour atteindre un terminal de données principal. Les obstacles sont des blocs de données physiques et des "Sentinelles" (ennemis).

### **3.2. Direction Artistique (Basée sur l'image reference)**

* **Style :** Pixel Art Isométrique Détaillé.  
* **Palette de Couleurs :** Nuit profonde (bleu/violet) contrastée par des néons vibrants (cyan, magenta, orange doré).  
* **Architecture :** Mix d'Afro-Futurisme et de Cyberpunk. Bâtiments futuristes avec des motifs tribaux géométriques, hiéroglyphes technologiques et végétation tropicale cybernétique.  
* **Protagoniste :** Silhouette d'homme avec dreadlocks, combinaison technologique lumineuse.  
* **Objets :** Les bombes sont des orbes de plasma lumineux. Les blocs destructibles sont des cubes technologiques complexes.

---

## **4\. BOUCLE DE GAMEPLAY CORE (CORE LOOP)**

1. **Début du Niveau :** Le joueur apparaît sur la grille isométrique. Les objectifs et le chrono démarrent.  
2. **Exploration & Destruction :** Le joueur déplace son personnage sur la grille. Il pose des bombes pour détruire les obstacles (Cubes Oranges/Violet) et révéler des passages ou des récompenses.  
3. **Gestion de la Menace :** Éviter ses propres explosions, les ennemis et les pièges. Utiliser les compétences.  
4. **Atteindre la Sortie :** Une fois le passage dégagé et les objectifs de données remplis, le joueur doit atteindre la "Porte de Données" avant la fin du temps.  
5. **Méta-Jeu :** Utiliser les ressources collectées pour améliorer le personnage et les compétences dans le hub centrale.

---

## **5\. MÉCANIQUES DE JEU**

### **5.1. Mouvements et Grille**

* **Perspective :** Vue isométrique à 3/4.  
* **Grille de Mouvement :** Le mouvement est fluide visuellement, mais le personnage s'aligne automatiquement sur une grille invisible (tiles) pour le placement précis des bombes et les collisions d'explosion (style "soft-grid").

### **5.2. Système de Bombes (Action de Base)**

* **Placement :** Le joueur appuie sur le bouton "BOMB" pour placer une bombe sur sa case actuelle.  
* **Délai d'Explosion :** Délai fixe (par ex. 3 secondes) avant l'explosion.  
* **Portée :** Par défaut, l'explosion se propage en forme de croix (N, S, E, O) sur 2 cases. Peut être amélioré.  
* **Physique :** Les bombes ne peuvent pas être traversées. Elles bloquent les ennemis et le joueur.

### **5.3. HUD et Économie en Cours de Partie (Haut de l'Écran)**

* **Score :** Points gagnés en détruisant des blocs et des ennemis, et en terminant rapidement.  
* **Temps (Time) :** Compte à rebours de niveau. Si le temps atteint 02:34 \-\> 00:00, le joueur perd une vie.  
* **Vies (Lives) :** Nombre d'essais restants (Ici, 3 cœurs). Perdre une vie (explosion, ennemi, temps) réinitialise le personnage au point de départ du niveau actuel, avec un temps réinitialisé.

---

## **6\. SYSTÈME DE COMPÉTENCES (HUD BAS)**

C'est l'élément stratégique principal du HUD de l'image.

### **6.1. Compétences PASSIVES (Slots de Gauche)**

Ces compétences s'activent de manière situationnelle ou ont un effet constant, sans coût d'utilisation direct, mais peuvent avoir des jauges d'activation (les barres de couleur à côté de l'icône).

* **1\. SPEED BOOST (Botte Ailée) :**  
  * **Effet :** Augmente la vitesse de déplacement de base du personnage de 25%.  
  * **Jauge de Jeton :** La jauge à côté se remplit en marchant. Une fois pleine, le joueur peut activer un "Sprint" temporaire de 3 secondes (en double-cliquant sur le joystick, par exemple).  
  * **Amélioration :** Augmente la vitesse, le temps de sprint, ou la vitesse de charge.  
* **2\. SHIELD (Bouclier) :**  
  * **Effet :** Fournit une protection automatique contre UNE seule explosion (propre bombe ou ennemi) ou UN impact ennemi.  
  * **Jauge de Jeton :** Se recharge lentement après utilisation. La jauge indique la progression de la recharge.  
  * **Amélioration :** Réduit le temps de recharge, ajoute un deuxième bouclier, ou ajoute une explosion de zone (AoE) lors de la rupture du bouclier.

### **6.2. Compétences ACTIVES (Slots de Droite)**

Ces compétences ont un effet instantané puissant et sont gérées par un *Cooldown* (temps de recharge) visible.

* **3\. BOMB (Orbe Rouge avec 5.0s Cooldown) :**  
  * **Effet :** C'est le bouton principal de pose de bombe. Appuyer pose une bombe standard.  
  * **Cooldown :** 5 secondes entre chaque pose de bombe.  
  * **Gestion de l'Amélioration :** Le joueur commence avec 1 bombe simultanée. Les améliorations débloquent plus de slots de bombes simultanées, mais le cooldown de base de 5 secondes s'applique à *chaque* slot. (ex: si le joueur a 2 slots de bombe, il peut en poser une, cooldown de 5s sur ce slot commence, et il peut en poser une autre, le cooldown de 5s sur le deuxième slot commence).  
* **4\. SPECIAL (Orbe Violet avec 5.0s Cooldown) :**  
  * **Effet :** Une action puissante non-liée à la bombe standard.  
  * **Type de Compétence :** Le joueur choisit *une* compétence Spéciale parmi plusieurs avant le niveau (ex: *TÉLÉPORTATION* courte distance à travers un bloc, *CHAÎNE D'ÉCLAIRS* pour détruire 3 blocs en ligne droite instantanément, *CONTRÔLEUR DE BOMBE* pour faire exploser manuellement la dernière bombe posée). L'image montre un symbole d'énergie central qui suggère une explosion d'énergie pure autour du personnage.  
  * **Cooldown :** 5 secondes de recharge fixe.

---

## **7\. CONTRÔLES (MODE MOBILE)**

* **Perspective :** Paysage.  
* **Mouvement (Gauche) :** Un joystick virtuel transparent dans le coin inférieur gauche pour un contrôle analogique ou d'angle précis (compatible avec la grille).  
* **Actions (Droite) :** Les 4 boutons de compétences (2 Actifs clairs, 2 Passifs informatifs) disposés ergonomiquement pour le pouce droit.

---

## **8\. ÉVOLUTION DU JEU (LEVEL DESIGN TACTIQUE)**

L'image montre un niveau de base. L'évolution se fera par :

### **8.1. Types d'Obstacles (Blocs)**

* **Blocs de Base (Cubes Oranges) :** Destructibles par une bombe. Révèlent des pièces de données (Data Coins).  
* **Blocs Renforcés (Cubes Violets) :** Nécessitent deux explosions pour être détruits.  
* **Murs Indestructibles (Bâtiments et Plateformes Néon) :** Structure de base du labyrinthe.  
* **Blocs Mobiles :** Des plateformes qui se déplacent sur des rails de grille.

### **8.2. Ennemis (Sentinelles de Sombra-Corps)**

* **"Patrouilleur Basic" :** Suit un chemin fixe, collision mortelle, pas intelligent.  
* **"Bombardier-Drone" :** Tire des orbes d'énergie à distance.  
* **"Chasseur-Néon" :** Plus rapide, cible le joueur si dans la ligne de mire.

### **8.3. Objectifs de Niveau**

* **"Collecter 10 Nodes" :** Détruire des blocs pour trouver des nodes de données cachés.  
* **"Éliminer toutes les Sentinelles" :** Nettoyer le niveau.  
* **"Survival" :** Survivre 3 minutes contre des vagues infinies d'ennemis.

---

## **9\. MÉTA-JEU ET PROGRESSION (LE HUB)**

Après chaque niveau, le joueur retourne à "Ébène-Prime Hub".

### **9.1. Économie**

* **Data Coins :** Collectés en mission (en détruisant des blocs et des ennemis). Monnaie de base.  
* **Cœurs Néon (Premium) :** Achetés avec de l'argent réel ou gagnés lors d'événements.

### **9.2. Améliorations (The Tech Tree)**

Le joueur utilise ses Data Coins pour améliorer ses compétences :

* **Bomb Tech :** Portée d'explosion (+1 case), Dégâts (+), Slots de bombes (+).  
* **Passives Tech :** Réduire le Cooldown du Shield, augmenter la vitesse max, etc.  
* **Actives Tech :** Débloquer de nouvelles compétences "Spéciales".

### **9.3. Personnalisation (Skins)**

Le joueur peut acheter des skins pour son Speed-Runner avec des Cœurs Néon :

* Costumes Cyberpunk différents.  
* Couleurs de néon de trail.  
* Effets visuels de bombe.

---

## **10\. RISQUES TECHNIQUE ET DÉFIS**

* **Collision en Isométrie :** S'assurer que le joueur ne reste pas coincé dans les coins isométriques, et que le placement des bombes sur les tiles est parfaitement aligné visuellement.  
* **Lisibilité du Cooldown :** Maintenir les timers de 5.0s et les jauges passives clairs et lisibles même lors de l'action intense.  
* **Performance Mobile :** Le Pixel Art détaillé avec de nombreux effets de lumière néon peut être gourmand en ressources sur des appareils mobiles bas de gamme.

