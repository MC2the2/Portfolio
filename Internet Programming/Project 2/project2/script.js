var creditCard = "First Name: <input type='text' name='firstname' id='Credit' required><br>Last Name: <input type='text' name='lastname' id = 'Credit' required><br>Address: <input type='text' name='Address' id='Credit' required><br>City: <input type='text' name='lastname' id = 'Credit' required><br>Zip: <input type='text' name='zip' id='ZIP' required ><br>Name On Card: <input type='text' name='nameoncard' id = 'Credit' required><br>Credit Card Number: <input type='text' name='cardnumber' id='cardNumber' required><br><a href='https://en.wikipedia.org/wiki/Card_security_code' target='_blank'>CVV2/CVC</a> <input type='text' name='ccv' id = 'CCV' required><br>State: <select name='state' id='selectState'><option value = 'Select state'>Select state</option><option value = 'Alabama'>Alabama</option><option value = 'Alaska'>Alaska</option><option value = 'Arizona'>Arizona</option><option value = 'Arkansas'>Arkansas</option><option value = 'California'>California</option><option value = 'Colorado'>Colorado</option><option value = 'Connecticut'>Connecticut</option><option value = 'Delaware'>Delaware</option><option value = 'Florida'>Florida</option><option value = 'Georgia'>Georgia</option><option value = 'Hawaii'>Hawaii</option><option value = 'Idaho'>Idaho</option><option value = 'Illinois'>Illinois</option><option value = 'Indiana'>Indiana</option><option value = 'Iowa'>Iowa</option><option value = 'Kansas'>Kansas</option><option value = 'Kentucky'>Kentucky</option><option value = 'Louisiana'>Louisiana</option><option value = 'Maine'>Maine</option><option value = 'Maryland'>Maryland</option><option value = 'Massachusetts'>Massachusetts</option><option value = 'Michigan'>Michigan</option><option value = 'Minnesota'>Minnesota</option><option value = 'Mississippi'>Mississippi</option><option value = 'Missouri'>Missouri</option><option value = 'Montana'>Montana</option><option value = 'Nebraska'>Nebraska</option><option value = 'Nevada'>Nevada</option><option value = 'New Hampshire'>New Hampshire</option><option value = 'New Jersey'>New Jersey</option><option value = 'New Mexico'>New Mexico</option><option value = 'New York'>New York</option><option value = 'North Carolina'>North Carolina</option><option value = 'North Dakota'>North Dakota</option><option value = 'Ohio'>Ohio</option><option value = 'Oklahoma'>Oklahoma</option><option value = 'Oregon'>Oregon</option><option value = 'Pennsylvania'>Pennsylvania</option><option value = 'Rhode Island'>Rhode Island</option><option value = 'South Carolina'>South Carolina</option><option value = 'South Dakota'>South Dakota</option><option value = 'Tennessee'>Tennessee</option><option value = 'Texas'>Texas</option><option value = 'Utah'>Utah</option><option value = 'Vermont'>Vermont</option><option value = 'Virginia'>Virginia</option><option value = 'Washington'>Washington</option><option value = 'West Virginia'>West Virginia</option><option value = 'Wisconsin'>Wisconsin</option><option value = 'Wyoming'>Wyoming</option></select><br>Expiry: <input type='month' id = 'Month' name='creditexpiration' min='2017-01' max='2020-12' value='2019-04'>";

var payPal = "<br> E-mail Address: <input type='text' name='emailaddress' id='EMail' required ><br>Password: <input type='password' name='passwerd' id='password' required><br>";

function testLength(value, length, exactLength){
	if (exactLength == true){
		if (value.length == length){
			return true;
		}
		else
		{
			return false;
		}
	}
	else{
		if (value.length >= length){
			return true;
		}
		else
		{
			return false;
		}
	}
}
function testNumber(value){
	if (isNaN(value)){
		return false;
	}
	else
	{
		return true;
	}
}
function updateForm(radioValue){
	if (radioValue.value == 'PayPal') {
		document.getElementById("Payment").innerHTML = payPal;
	}
	else
	{
		document.getElementById("Payment").innerHTML = creditCard;
	}
}
function validateControl(control, name, length){
	if (testLength(control.value, length, true) == false)
	{
		alert('The control ' + name + ' does not have enough characters.  Please enter a valid ' + name + ' value.');
		return false;
	}
	else if (testNumber(control.value) == false){
		alert('The control ' + name + ' is not numeric.  Please enter a valid ' + name + ' value.');
		return false;
	}
	return true;

}
function validateCreditCard(value){
	value = value.replace(/ /g, '');
	string = '3456';
	if (testNumber(value) == false){
		alert('The credit card number is not a number.');
		return false;
	}
	else if (string.indexOf(value.charAt(0)) == -1){
		alert('The credit card number does not begin with a 3, 4, 5, or 6.');
		return false;
	}
	else if (value.charAt(0) == '3'){
		if (testLength(value, 15, true) == false){
			alert('The credit card number is not of the correct length.  All card values that begin with 3 need to have fifteen numbers and all other beginning numbers need to have 16 numbers.');
			return false;
		}
	}
	else if (testLength(value, 16, true) == false){
		alert('The credit card number is not of the correct length.  All card values that begin with 3 need to have fifteen numbers and all other beginning numbers need to have 16 numbers.');
		return false;
	}
	return true;
	}
		
		
function validateForm()
{	
	var result = false;
	var creditCardForm = document.getElementById("Credit");
	
	var zip = document.getElementById("ZIP");
	var ccv = document.getElementById("CCV");
	var credit = document.getElementById("cardNumber");
	var state = document.getElementById("selectState");
	var month = document.getElementById("Month");
	
	var email = document.getElementById("EMail");
	var pass = document.getElementById("password");
	
	if (creditCardForm.checked){ // Credit card payment option selected
		if (validateControl(zip, 'ZIP', 5) == true){ // Zip Code
			if (validateControl(ccv, 'CVV2/CVC', 3) == true){ // CVV2/CVC
				if (validateCreditCard(credit.value) == true){ // Credit Card
					if (validateState(state.selectedIndex) == true) { // State
						if(validateDate(month.value) == true){ // Month
							// All of the credit card fields are good
							result = true; 
						}
						
					}
				}
			}
		}
	}
	else{ // PayPal payment option selected
		if (validateEmail(email.value) == true){
			if (validatePassword(pass.value) == true){
				// All of the PayPal fields are good
				result = true; 
			}
		}
	}
	if(result == true){
		alert("Form Submitted");
	}
	return false;
}
function validateState(value){
	if (value == 0){
		alert("A state has not been selected.  Please select a state");
		return false;	
	}
	else{
		return true;
	}
}

function validatePassword(value){
	if (testLength(value, 6, false) == true){
		return true;
	}
	else{
		alert("The password needs to be at least 6 characters.");
		return false;
		}
	}

function validateDate(value){
	var d = new Date();
	expiry = value.split("-");
	expiryDate = new Date(expiry[0], expiry[1]);
	if (expiryDate > d){
		return true;
	}
	else{
		alert("The expiration date has to be in the future.");
		return false;
	}
}

function validateEmail(value) {
	if (/^\w+([\.-]?\w+)*@\w+([\.-]?\w+)*(\.\w{2,3})+$/.test(value)){
		return true;
	}
	else{
		alert('The email address is in the incorrect format.  The correct format is *@*.*.');
		return false;
	}
	
}
