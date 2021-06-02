﻿using System.Collections;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using TMPro;
using UnityEngine.SceneManagement;

public class FirebaseManager : MonoBehaviour {

    // Singleton
    public static FirebaseManager instance;

    // Firebase variables
    [Header("Firebase")]
    [SerializeField] DependencyStatus dependencyStatus;
    [SerializeField] FirebaseAuth auth;
    [SerializeField] FirebaseUser User;
    [SerializeField] DatabaseReference DBref;
    Firebase.FirebaseApp firebaseApp;

    // Login variables
    [Header("Login")]
    [SerializeField] TMP_InputField emailLoginField = null;
    [SerializeField] TMP_InputField passwordLoginField = null;
    [SerializeField] TMP_Text messageText = null;

    // Register variables
    [Header("Register")]
    [SerializeField] TMP_InputField usernameRegisterField = null;
    [SerializeField] TMP_InputField emailRegisterField = null;
    [SerializeField] TMP_InputField passwordRegisterField = null;
    [SerializeField] TMP_InputField passwordRegisterVerifyField = null;


    void Awake() {
        // Singleton
        if (instance == null)
            instance = this;
        else {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);

        // Check that all of the necessary dependencies for Firebase are present on the system
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
            dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available) {
                // If they are avalible Initialize Firebase
                InitializeFirebase();
            }
            else {
                Debug.LogError("Could not resolve all Firebase dependencies: " + dependencyStatus);
            }
        });

        // Flag that user not logged in!
        PlayerPrefs.SetInt("loggedIN", 0);
    }

    //  -------------------------------------------  //
    //  ---------------   BUTTONS   ---------------  //
    //  -------------------------------------------  //

    // Function for the login button
    public void LoginButton() {
        // Call the login coroutine passing the email and password
        StartCoroutine(Login(emailLoginField.text, passwordLoginField.text));
    }
    //Function for the register button
    public void RegisterButton() {
        // Call the register coroutine passing the email, password, and username
        StartCoroutine(Register(emailRegisterField.text, passwordRegisterField.text, usernameRegisterField.text));
    }


    //  -------------------------------------------  //
    //  -----------   PUBLIC METHODS   ------------  //
    //  -------------------------------------------  //


    public void SignOut() { auth.SignOut(); }


    //  -------------------------------------------  //
    //  -----------   PRIVATE METHODS   -----------  //
    //  -------------------------------------------  //

    // Setting up the Firebase Connections
    void InitializeFirebase() {
        Debug.Log("Setting up Firebase Auth");
        //Set the authentication instance object
        auth = FirebaseAuth.DefaultInstance;
        DBref = FirebaseDatabase.DefaultInstance.RootReference;
        // Update Google Services 
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available) {
                // Create and hold a reference to your FirebaseApp,
                // where app is a Firebase.FirebaseApp property of your application class.
                firebaseApp = Firebase.FirebaseApp.DefaultInstance;

                // Set a flag here to indicate whether Firebase is ready to use by your app.
            }
            else {
                UnityEngine.Debug.LogError(System.String.Format(
                  "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
                // Firebase Unity SDK is not safe to use here.
            }
        });

    }

    IEnumerator ShowMessage(string _message, float time) {
        messageText.text = _message;
        yield return new WaitForSeconds(time);
        messageText.text = "";
    }

    IEnumerator DelayLogin() {
        StartCoroutine(ShowMessage("Logged In", 1));
        yield return new WaitForSeconds(1);
        // flag as logged in so that player can see the menu when Menu scene loaded after death
        PlayerPrefs.SetInt("loggedIN", 1);
        FindObjectOfType<MenuHandler>().LoginSuccess();
    }

    #region Updates

    IEnumerator Login(string _email, string _password) {
        // Call the Firebase auth signin function passing the email and password
        var LoginTask = auth.SignInWithEmailAndPasswordAsync(_email, _password);
        // Wait until the task completes
        yield return new WaitUntil(predicate: () => LoginTask.IsCompleted);

        if (LoginTask.Exception != null) {
            // If there are errors handle them
            Debug.LogWarning(message: $"Failed to register task with {LoginTask.Exception}");
            FirebaseException firebaseEx = LoginTask.Exception.GetBaseException() as FirebaseException;
            AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

            string message = "Login Failed!";
            switch (errorCode) {
                case AuthError.MissingEmail:
                    message = "Missing Email";
                    break;
                case AuthError.MissingPassword:
                    message = "Missing Password";
                    break;
                case AuthError.WrongPassword:
                    message = "Wrong Password";
                    break;
                case AuthError.InvalidEmail:
                    message = "Invalid Email";
                    break;
                case AuthError.UserNotFound:
                    message = "Account does not exist";
                    break;
                default:
                    messageText.text = message;
                    break;
            }
            StartCoroutine(ShowMessage(message, 3));
        }
        else {
            // User is now logged in
            // Now get the result
            User = LoginTask.Result;
            Debug.LogFormat("User signed in successfully: {0} ({1})", User.DisplayName, User.Email);
            messageText.text = "";
            StartCoroutine(DelayLogin());
        }
    }

    IEnumerator Register(string _email, string _password, string _username) {
        if (_username == "") {
            // If the username field is blank show a warning
            StartCoroutine(ShowMessage("Missing Username", 3));
        }
        else if (passwordRegisterField.text != passwordRegisterVerifyField.text) {
            // If the password does not match show a warning
            StartCoroutine(ShowMessage("Password Does Not Match!", 3));
        }
        else {
            // Call the Firebase auth signin function passing the email and password
            var RegisterTask = auth.CreateUserWithEmailAndPasswordAsync(_email, _password);
            // Wait until the task completes
            yield return new WaitUntil(predicate: () => RegisterTask.IsCompleted);

            if (RegisterTask.Exception != null) {
                // If there are errors handle them
                Debug.LogWarning(message: $"Failed to register task with {RegisterTask.Exception}");
                FirebaseException firebaseEx = RegisterTask.Exception.GetBaseException() as FirebaseException;
                AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

                string message = "Register Failed!";
                switch (errorCode) {
                    case AuthError.MissingEmail:
                        message = "Missing Email";
                        break;
                    case AuthError.MissingPassword:
                        message = "Missing Password";
                        break;
                    case AuthError.WeakPassword:
                        message = "Weak Password";
                        break;
                    case AuthError.EmailAlreadyInUse:
                        message = "Email Already In Use";
                        break;
                }
                StartCoroutine(ShowMessage(message, 3));
            }
            else {
                // User has now been created
                // Now get the result
                User = RegisterTask.Result;

                if (User != null) {
                    // Create a user profile and set the username
                    UserProfile profile = new UserProfile { DisplayName = _username };

                    // Call the Firebase auth update user profile function passing the profile with the username
                    var ProfileTask = User.UpdateUserProfileAsync(profile);
                    // Wait until the task completes
                    yield return new WaitUntil(predicate: () => ProfileTask.IsCompleted);

                    if (ProfileTask.Exception != null) {
                        //If there are errors handle them
                        Debug.LogWarning(message: $"Failed to register task with {ProfileTask.Exception}");
                        FirebaseException firebaseEx = ProfileTask.Exception.GetBaseException() as FirebaseException;
                        AuthError errorCode = (AuthError)firebaseEx.ErrorCode;
                        StartCoroutine(ShowMessage("Username Set Failed!", 3));
                    }
                    else {
                        // Write username to the database
                        StartCoroutine(UpdateUsernameAuth(_username));
                        StartCoroutine(UpdateUsernameDatabase(_username));
                        // Username is now set                        
                        StartCoroutine(ShowMessage("Registration is successful", 1));
                        FindObjectOfType<MenuHandler>().RegisterSuccess();
                    }
                }
            }
        }
    }

    IEnumerator UpdateUsernameAuth(string _username) {
        // Create a user profile and set the username
        UserProfile profile = new UserProfile { DisplayName = _username };

        // Call the Firebase auth update user profile function passing the profile with the username
        var ProfileTask = User.UpdateUserProfileAsync(profile);
        // Wait until the task completes
        yield return new WaitUntil(predicate: () => ProfileTask.IsCompleted);

        if (ProfileTask.Exception != null) {
            Debug.LogWarning(message: $"Failed to register task with {ProfileTask.Exception}");
        }
        else {
            // Auth username is now updated
        }
    }

    IEnumerator UpdateUsernameDatabase(string _username) {
        // Set the currently logged in user username in the database
        var DBTask = DBref.Child("users").Child(User.UserId).Child("username").SetValueAsync(_username);

        yield return new WaitUntil(predicate: () => DBTask.IsCompleted);

        if (DBTask.Exception != null) {
            Debug.LogWarning(message: $"Failed to register task with {DBTask.Exception}");
        }
        else {
            // Database username is now updated
        }
    }

    #endregion
}