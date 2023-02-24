function addMovie(movieID){
	window.location.replace("./index.php?action=add&movie_id=" + movieID);
	return true;
}

function confirmCheckout(){
	var value = confirm("Do you want to check out?");
	if (value == true){
		window.location.replace("./index.php?action=checkout");
		return true;
	}
	else{
		return false;
	}
	
}

function confirmLogout(){
	var value = confirm("Do you want to log off?");
	if (value == true){
		window.location.replace("./logon.php?action=logoff");
		return true;
	}
	else{
		return false;
	}
	
}

function confirmRemove(title, movieID){
var value = confirm("Do you want to remove" + title + "?");
	if (value == true){
		window.location.replace("./index.php?action=remove&movie_id=" + movieID);
		return true;
	}
	else{
		return false;
	}
}

