# Checklist de soumission — ReadLens

Date limite : **mercredi 18 novembre 2026, 21:00 (Paris)** = 12:00 PT. Aucune modification après.
Objectif : **tout soumettre mercredi avant 18:00**, pour garder 3 h de marge.
Détails techniques du Dashboard : [SUBMISSION.md](SUBMISSION.md). Textes : [DEVPOST.md](DEVPOST.md). Vidéo : [VIDEO.md](VIDEO.md).

## 0. Tout de suite

- [ ] Sauvegarder `~/.android-keystores/readlens-release.keystore` **et** `readlens-release.txt` (gestionnaire de mots de passe ou clé USB). Sans eux : plus aucune mise à jour possible.
- [ ] Décider qui figure dans l'équipe Devpost. La personne porteuse de l'idée :
  - soit membre de l'équipe : 18 ans ou plus, compte Meta avec accès développeur, non résidente du Brésil, du Québec ou d'un pays sous sanctions ;
  - soit hors équipe : avoir un accord écrit simple avec elle (l'idée vient d'elle, et c'est toi qui soumets).
- [ ] La personne qui soumet (le « Representative ») est membre du programme Meta VR Start. ✔ confirmé
- [ ] Sur Devpost : « Join Hackathon », puis créer le projet en brouillon.

## 1. Tester la version finale dans le casque (avec Claude)

- [ ] Dire à Claude quand le casque est libre. Il désinstalle l'ancienne version (signée avec la clé de test), puis installe la nouvelle.
- [ ] « Find your comfort » se lance au premier démarrage.
- [ ] La lecture d'une page marche **sans Internet** (la permission réseau a été retirée). Test : couper le Wi-Fi du casque.
- [ ] Effet scan, icônes, sons, animations, « Reduce motion ».
- [ ] Les pages sont toujours là après avoir quitté puis relancé l'app.
- [ ] Fluidité correcte (au moins 60 images/s, 90 visé).
- [ ] Corrections éventuelles, puis build final.

## 2. Dashboard Meta (environ 30 min)

- [ ] Créer l'app « ReadLens » (Meta Horizon Store).
- [ ] Renseigner les infos minimales : nom, description courte, image d'en-tête (Claude peut préparer texte et image).
- [ ] Tranche d'âge : **Teens and Adults**.
- [ ] Créer un **nouveau** canal de publication nommé exactement **`Competition`**.
- [ ] Envoyer `Builds/ReadLens.apk` sur ce canal. L'app est **gratuite**.
- [ ] Paramètres du canal : « Grant access to users by URL » + « Automatic approval ». Copier l'URL d'invitation.
- [ ] Tester l'URL avec **un autre compte Meta** (adulte, hors de ton organisation) : rejoindre → installer → lancer → capturer une page.

## 3. Vidéo (moins de 3:00, viser 2:15–2:30)

- [ ] Imprimer les pages de [DEMO_PAGES.md](DEMO_PAGES.md) en 16–18 pt.
- [ ] Préparer le lieu de tournage :
  - bonne lumière ;
  - **aucune personne identifiable** à part toi : pas de photos de gens au mur, pas de reflet de quelqu'un d'autre ;
  - **aucune marque ou logo visible** (écrans, produits, emballages).
- [ ] Faire un essai de 10 secondes pour vérifier que la fenêtre et la pièce apparaissent bien dans l'enregistrement.
- [ ] Filmer plan par plan dans le casque, en suivant [VIDEO.md](VIDEO.md). Pas d'images générées par IA.
- [ ] Monter la vidéo et ajouter la voix off en anglais (ou des sous-titres anglais). Les meilleurs moments d'abord.
- [ ] La publier sur YouTube **en public** (ou Vimeo), puis copier le lien.

## 4. Formulaire Devpost (tout en anglais)

- [ ] Claude met [DEVPOST.md](DEVPOST.md) à jour avec les nouveautés (sons, scan, pages conservées, icônes).
- [ ] Nom : ReadLens. Choisir l'accroche (140 caractères maximum).
- [ ] Track : **Productivity**. Division : **New Experience**.
- [ ] Description d'environ 500 mots : inspiration, comment c'est construit, suite prévue, **date de lancement visée**.
- [ ] Section « Hand Interactions » (optionnelle, mais à remplir).
- [ ] Instructions de test pour les juges, en anglais.
- [ ] Lien de la vidéo et lien d'invitation du canal Competition.
- [ ] Membres de l'équipe : noms, emails, rôles.
- [ ] Captures d'écran prises dans le casque (recommandées).
- [ ] **Soumettre** (pas seulement enregistrer le brouillon), puis vérifier que le statut est bien « Submitted ».

## 5. Après la soumission

- [ ] Ne plus rien envoyer sur le canal `Competition` : les juges évaluent l'état au moment de la date limite.
- [ ] Laisser l'app disponible jusqu'à l'annonce des gagnants (vers le 11 décembre).
- [ ] Surveiller tes emails : les gagnants doivent répondre aux documents demandés par Meta.
