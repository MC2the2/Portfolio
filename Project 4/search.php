<?php

session_start(); // Connect to the existing session
processPageRequest(); // Call the processPageRequest() function
ini_set('display_startup_errors', 1); // Report compiler (syntax) errors
ini_set('display_errors', 1); // Report run-time errors
error_reporting(E_ALL); // Do not hide any errors

function processingPageRequest(){

if (isset($_POST['searchString'])) {
		displaySearchResults($_POST["searchString"]);
		
}
else{
     require_once "./Template/search_form.html";   

}
}

function displaySearchResults($searchString){
	$results = file_get_contents('http://www.omdbapi.com/?apikey=1eb23cd5&s='.urlencode([$searchString]).'&type=movie&r=json');
	$movieArray = json_decode($results, true)["Search"];
	require_once "./Template/results_form.html";
	
}
?>
	
