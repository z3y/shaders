// using System.Collections;
// using System.Collections.Generic;
// using UnityEditor;
// using UnityEditor.PackageManager;
// using UnityEditor.PackageManager.Requests;
// using UnityEngine;

// public class UpdatePacakge
// {
//     static AddRequest installRequest;
//     static RemoveRequest removeRequest;

//     [MenuItem("Tools/z3y/Update")]
//     public static void Update()
//     {
//         removeRequest = Client.Remove("com.z3y.shaders");
//         EditorApplication.update += Remove;
//     }
//     private static void Remove()
//     {
//         if (removeRequest.IsCompleted)
//         {
//             installRequest = Client.Add("https://github.com/z3y/ShadersPrivate.git");
//             EditorApplication.update += Install;

//             EditorApplication.update -= Remove;
//         }
//     }

//     private static void Install()
//     {
//         if (installRequest.IsCompleted)
//         {
//             if (installRequest.Status == StatusCode.Success)
//                 Debug.Log("Installed: " + installRequest.Result.packageId);
//             else if (installRequest.Status >= StatusCode.Failure)
//                 Debug.Log(installRequest.Error.message);

//             EditorApplication.update -= Install;
//         }
//     }
// }
