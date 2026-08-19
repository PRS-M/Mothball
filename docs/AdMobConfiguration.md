# AdMob Configuration

Debug builds always use Google's official test application ID and test ad-unit IDs. Do not test with production units during development.

Release builds read production IDs from local files that are intentionally ignored by Git. The IDs are included in the distributed app and are therefore not secret credentials, but this arrangement keeps them out of the public repository.

## Local Release Builds

Create the local files from the committed templates:

```bash
cp src/MothballMobile/Properties/AdMob.Release.props.example src/MothballMobile/Properties/AdMob.Release.props
cp src/MothballMobile/Properties/appsettings.Release.example.json src/MothballMobile/Properties/appsettings.Release.json
```

Set the iOS application ID in `AdMob.Release.props`:

```xml
<AdMobApplicationId>ca-app-pub-YOUR_PUBLISHER_ID~YOUR_APPLICATION_ID</AdMobApplicationId>
```

Set the iOS app-open and banner unit IDs in `appsettings.Release.json`:

```json
{
  "AdMob": {
    "AppOpenAdUnitId": "ca-app-pub-YOUR_PUBLISHER_ID/YOUR_APP_OPEN_UNIT_ID",
    "BannerAdUnitId": "ca-app-pub-YOUR_PUBLISHER_ID/YOUR_BANNER_UNIT_ID"
  }
}
```

`AdMob.Release.props` provides the build-time value required in the generated iOS `Info.plist`. `appsettings.Release.json` is packaged as a MAUI asset and provides the unit IDs at runtime. A Release app throws a clear startup error when the JSON file is missing or incomplete.

## CI Builds

In CI, create the same two files from encrypted CI variables before building. Alternatively, pass the application ID as an MSBuild property:

```bash
dotnet build src/MothballMobile/MothballMobile.csproj -c Release -f net10.0-ios \
  -p:AdMobApplicationId=ca-app-pub-YOUR_PUBLISHER_ID~YOUR_APPLICATION_ID
```

The runtime JSON file is still required for Release banner and app-open units.

## Do Not Store

Never add Google service-account JSON keys, OAuth client secrets, AdMob API refresh tokens, or backend credentials to either file. Those are actual secrets and belong in a backend or the CI provider's encrypted secret store.