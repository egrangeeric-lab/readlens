# Submitting ReadLens

Deadline: **Wed Nov 18 2026, 12:00 PT (21:00 Paris)**. Do steps 1–4 at least a day early: uploads are validated and
the invite link needs testing from a second account.

## 0. Build the release APK

```bash
U=/Applications/Unity/Hub/Editor/6000.6.5f1/Unity.app/Contents/MacOS/Unity
$U -batchmode -quit -buildTarget Android -projectPath . -executeMethod ReadLens.EditorTools.Bootstrap.Build -logFile build.log
grep "\[ReadLens\]" build.log   # must say "Release signing with ..." and "Build Succeeded"
~/Library/Android/sdk/build-tools/*/apksigner verify --print-certs Builds/ReadLens.apk   # CN=ReadLens, not "Android Debug"
```

- Signing key: `~/.android-keystores/readlens-release.keystore`, passwords in `~/.android-keystores/readlens-release.txt`.
  **Back both up somewhere safe (password manager).** Every future update must be signed with the same key.
  Never commit them.
- `versionCode` = minutes since 2026-01-01 UTC, so every build is accepted as newer. `versionName` = 1.0.0.

The headset currently has a debug-signed build. Android refuses to update an app with a different signature, so once:

```bash
adb uninstall com.readlens.quest   # deletes the saved reading profile and pages on the headset
adb install -r -g Builds/ReadLens.apk
```

## 1. Create the app

1. Open the [Developer Dashboard](https://developers.meta.com/horizon/manage/) with the account of the
   organization registered for the competition (Start program member).
2. **Create a new app** → app name `ReadLens` → platform **Meta Horizon Store** → **Create**.
3. The app is tied to the package name of the first uploaded APK (`com.readlens.quest`): always upload that package.
4. Note the **App ID** (left menu **API**). Fill the minimum App Metadata (name, short description, a header image):
   the invite page shows them.
5. Age group self-certification is mandatory before release. Decide it consciously, it is not a formality:
   - `Teens and Adults (13+)`: no extra integration needed.
   - `Mixed ages` (under 13 and 13+): requires the Platform SDK *Get Age Category* API, which ReadLens does not integrate.
   - `Children` (under 13): COPPA obligations.
   ReadLens is meant for kids, but the competition build is tested by adult judges; the fastest compliant choice for
   the competition is Teens and Adults. Revisit before a public launch.

## 2. Create the `Competition` release channel

**Distribution → Release Channels → Create Release Channel**, name exactly `Competition` (the rules require a *new*
channel with this name; do not use ALPHA/BETA).

## 3. Upload the APK

**Web:** **Distribution → Builds → Upload** (or **Upload new build** on the `Competition` channel page) →
select `Builds/ReadLens.apk` → channel `Competition`, age group as decided above, short release notes →
fix anything the pre-upload validation reports → upload.

**CLI alternative** ([Platform Command Line Utility](https://developers.meta.com/horizon/resources/publish-reference-platform-command-line-utility/), macOS binary from the Meta downloads page, `chmod +x`):

```bash
./ovr-platform-util upload-quest-build \
  --app-id <APP_ID> --app-secret <APP_SECRET> \
  --apk Builds/ReadLens.apk --channel Competition \
  --age-group TEENS_AND_ADULTS \
  --notes "Competition build"
```

`<APP_ID>` is on the app's **API** page; `<APP_SECRET>` is the app secret shown there (or pass a user `--token`
instead). Never commit either. Channel names are not case-sensitive.

## 4. Invite URL, then test it from another account

1. **Distribution → Release Channels → Competition → Channel Settings**.
2. **Channel Invite** → tick **Grant access to users by URL** → **Automatic approval** → **Save Changes**.
3. Copy the URL. The link stops working for new users 90 days after the last upload to the channel (fine: judging
   ends before that), and channels hold 200 users by default.
4. Test with a **different Meta account** (not a member of the organization), 18+: open the URL on a phone or computer
   → join → **Install on headset**. On a headset signed in with that account, the app appears in the Library.
   Launch it, allow camera access, capture a page, quit, relaunch: pages must still be there.

## 5. Devpost

On [start-developer-competition-26.devpost.com](https://start-developer-competition-26.devpost.com/) → your project:

| Field | What to paste |
| --- | --- |
| Link to the project | The `Competition` channel invite URL from step 4 |
| Video | Public YouTube or Vimeo link, under 3:00, footage from the headset (no identifiable child) |
| Description | Inspiration, how it was built (Unity 6, Meta XR SDKs, Passthrough Camera API, on-device ML Kit OCR, no cloud), future plans, a target launch date |
| Track / Division | Productivity / New Experience |
| Testing instructions | Hands only, seated. Allow camera access. Hold a printed page about 40 cm away until *Page detected* appears, press *Read this page* (or tap thumb on index). Thumb swipe turns pages. Settings tab changes the reading profile. |

Everything must be in English. Keep the build in the channel until the winner announcement.

## Sources

- [Rules, Devpost](https://start-developer-competition-26.devpost.com/rules) (Section 4, Submission)
- [Create an app (Platform SDK getting started)](https://developers.meta.com/horizon/documentation/unity/ps-get-started/)
- [Release channels](https://developers.meta.com/horizon/resources/publish-release-channels/)
- [Add users to a release channel](https://developers.meta.com/horizon/resources/publish-release-channels-add-users/)
- [Platform Command Line Utility](https://developers.meta.com/horizon/resources/publish-reference-platform-command-line-utility/)
- [Application manifest requirements](https://developers.meta.com/horizon/resources/publish-mobile-manifest/)
- [Application signing](https://developers.meta.com/horizon/resources/publish-mobile-app-signing/)
- [Age group self-certification](https://developers.meta.com/horizon/resources/age-groups/)
