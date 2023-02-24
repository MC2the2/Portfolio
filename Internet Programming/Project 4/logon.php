<?php
ini_set('display_startup_errors', 1); // Report compiler (syntax) errors
ini_set('display_errors', 1); // Report run-time errors
error_reporting(E_ALL); // Do not hide any errors

processingPageRequest();

function processingPageRequest(){
session_destroy;
session_unset;
if (isset($_POST['username']) && isset($_POST['password'])){
        authenticateUser($_POST['username'], $_POST['password']);
}
else{
        displayLogInForm();

}
}

function displayLogInForm($message=""){
        require_once "./Template/logon_form.html";
}

//$myfile = fopen("webdictionary.txt", "r") or die("Unable to open file!")

function authenticateUser($username, $password){
        $myfile = fopen("./Data/credentials.db", 'r');
        $array = explode(',', fgets($myfile));

        if ($array[0] == $username && $array[1] == $password){
                session_start();
                $_SESSION["displayname"] = $array[2];
                $_SESSION["email"] = $array[3];
                header("Location: index.php");
        }
        else {
                $error = "The user name and passwords have to match.";
                displayLogInForm($error);
        }

}

?>