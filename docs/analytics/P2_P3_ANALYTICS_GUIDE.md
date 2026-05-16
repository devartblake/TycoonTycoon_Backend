# Analytics & Crash Reporting Setup Guide

## Quick Setup (30 minutes - Firebase)

### 1. Firebase Console Setup (10 minutes)

1. Go to [Firebase Console](https://console.firebase.google.com)
2. Create new project or select existing
3. Add Android app:
   - Package name: `com.yourcompany.triviatycoon`
   - Download `google-services.json`
   - Place in `android/app/`
4. Add iOS app:
   - Bundle ID: `com.yourcompany.triviatycoon`
   - Download `GoogleService-Info.plist`
   - Place in `ios/Runner/`

### 2. Add Dependencies (5 minutes)

```yaml
# pubspec.yaml
dependencies:
  firebase_core: ^2.24.0
  firebase_analytics: ^10.7.0
  firebase_crashlytics: ^3.4.0
```

```bash
flutter pub get
```

### 3. Initialize Firebase (5 minutes)

**File:** `lib/main.dart`

```dart
import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_crashlytics/firebase_crashlytics.dart';
import 'package:flutter/foundation.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  
  await Firebase.initializeApp();
  
  // Pass all uncaught errors to Crashlytics
  FlutterError.onError = (errorDetails) {
    FirebaseCrashlytics.instance.recordFlutterFatalError(errorDetails);
  };
  
  // Pass all async errors to Crashlytics
  PlatformDispatcher.instance.onError = (error, stack) {
    FirebaseCrashlytics.instance.recordError(error, stack, fatal: true);
    return true;
  };
  
  runApp(MyApp());
}
```

### 4. Create Analytics Service (10 minutes)

**File:** `lib/core/services/analytics_tracking_service.dart`

```dart
import 'package:firebase_analytics/firebase_analytics.dart';
import 'package:firebase_crashlytics/firebase_crashlytics.dart';

class AnalyticsService {
  final FirebaseAnalytics _analytics = FirebaseAnalytics.instance;
  final FirebaseCrashlytics _crashlytics = FirebaseCrashlytics.instance;
  
  // Auth Events
  Future<void> logLogin(String method) async {
    await _analytics.logLogin(loginMethod: method);
  }
  
  Future<void> logSignup(String method) async {
    await _analytics.logSignUp(signUpMethod: method);
  }
  
  Future<void> logLogout() async {
    await _analytics.logEvent(name: 'logout');
  }
  
  // Game Events
  Future<void> logQuizStart({
    required String category,
    required String difficulty,
  }) async {
    await _analytics.logEvent(
      name: 'quiz_start',
      parameters: {
        'category': category,
        'difficulty': difficulty,
      },
    );
  }
  
  Future<void> logQuizComplete({
    required String category,
    required int score,
    required int totalQuestions,
  }) async {
    await _analytics.logEvent(
      name: 'quiz_complete',
      parameters: {
        'category': category,
        'score': score,
        'total_questions': totalQuestions,
        'accuracy': (score / totalQuestions * 100).toInt(),
      },
    );
  }
  
  Future<void> logPremiumPurchase(double price) async {
    await _analytics.logPurchase(
      value: price,
      currency: 'USD',
      items: [
        AnalyticsEventItem(
          itemName: 'Premium Subscription',
          itemCategory: 'subscription',
        ),
      ],
    );
  }
  
  // User Properties
  Future<void> setUserProperty(String name, String value) async {
    await _analytics.setUserProperty(name: name, value: value);
  }
  
  Future<void> setUserRole(String role) async {
    await setUserProperty('user_role', role);
  }
  
  Future<void> setPremiumStatus(bool isPremium) async {
    await setUserProperty('is_premium', isPremium.toString());
  }
  
  // Crash Reporting
  Future<void> logError(
    dynamic exception,
    StackTrace? stackTrace, {
    String? reason,
    bool fatal = false,
  }) async {
    await _crashlytics.recordError(
      exception,
      stackTrace,
      reason: reason,
      fatal: fatal,
    );
  }
  
  Future<void> setUserIdentifier(String userId) async {
    await _analytics.setUserId(id: userId);
    await _crashlytics.setUserIdentifier(userId);
  }
  
  Future<void> log(String message) async {
    await _crashlytics.log(message);
  }
}
```

### 5. Add Provider

**File:** `lib/game/providers/riverpod_providers.dart`

```dart
final analyticsServiceProvider = Provider<AnalyticsService>((ref) {
  return AnalyticsService();
});
```

### 6. Track Events

```dart
// In login flow
final analytics = ref.read(analyticsServiceProvider);
await analytics.logLogin('email');
await analytics.setUserRole('player');

// In quiz
await analytics.logQuizStart(
  category: 'Science',
  difficulty: 'medium',
);

// On error
try {
  // some code
} catch (e, stack) {
  await analytics.logError(e, stack, reason: 'Quiz load failed');
}
```

---

## Alternative: Sentry (Privacy-Focused)

### Setup (20 minutes)

```yaml
dependencies:
  sentry_flutter: ^7.0.0
```

```dart
import 'package:sentry_flutter/sentry_flutter.dart';

Future<void> main() async {
  await SentryFlutter.init(
    (options) {
      options.dsn = 'YOUR_SENTRY_DSN';
      options.tracesSampleRate = 1.0;
    },
    appRunner: () => runApp(MyApp()),
  );
}
```

---

## Key Events to Track

### Must Track:
- User signup
- User login
- Quiz start
- Quiz complete
- Errors/crashes

### Nice to Have:
- Screen views
- Feature usage
- Premium upgrades
- Social shares

---

## Testing

```dart
// In debug mode, log to console
if (kDebugMode) {
  print('[Analytics] Quiz started: Science');
}
await analytics.logQuizStart(category: 'Science');
```

View in Firebase Console:
- Analytics → Events (live events)
- Crashlytics → Crashes

---

## Done! 

Analytics tracking and crash reporting now active. 📊
