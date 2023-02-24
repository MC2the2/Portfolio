

function calcSum(array){
	var index = 0;
	var sum = 0;
	while(index < array.length){
		sum = sum + array[index];
		index = index + 1;
	}
	return Number(sum.toFixed(2));
}
function calcMean(array){
	return Number((calcSum(array)/array.length).toFixed(2));
}
function findMin(array){
	return Number(array[0].toFixed(2));
}
function findMax(array){
	return Number(array[array.length - 1].toFixed(2));
}
function calcMedian(array){
	var medianAmount = (array.length + 1)/2;
	return Number((array[Math.floor(medianAmount)-1] + (array[Math.floor(medianAmount)] - array[Math.floor(medianAmount)-1]) * (medianAmount%1)).toFixed(2));  
}
function calcVariance(array){
	var mean = calcMean(array);
	var sumsquares = 0;
	var index = 0;
	while(index < array.length){
		sumsquares = sumsquares + (array[index])**2;
		index = index + 1;
	}
	return Number((sumsquares/array.length - mean**2).toFixed(2));
	
}
function calcStdDev(array){
	return Number((Math.sqrt(calcVariance(array))).toFixed(2));
}
function calcMode(array){
	
	var modeArray = [];
	var index = 0;
	var previousValue = NaN;
	var recordOccurrences = 0;
	var uniqueNumberArray = [];
	while(index < array.length){
		if (array[index] != previousValue){
			uniqueNumberArray.push(array[index]);
			previousValue = array[index];
		}
		index = index + 1;
	}
	index = 0;
	while (index < uniqueNumberArray.length){
		if(index == 0){
			
			recordOccurrences = array.lastIndexOf(uniqueNumberArray[index])-array.indexOf(uniqueNumberArray[index]);
			if (recordOccurrences > 0){
				modeArray.push(uniqueNumberArray[index]);
		    }
		}
		else{			
			if(array.lastIndexOf(uniqueNumberArray[index])-array.indexOf(uniqueNumberArray[index]) > recordOccurrences){
				modeArray = [uniqueNumberArray[index]];
				recordOccurrences = array.lastIndexOf(uniqueNumberArray[index])-array.indexOf(uniqueNumberArray[index]);
			}
			else if (array.lastIndexOf(uniqueNumberArray[index])-array.indexOf(uniqueNumberArray[index]) == recordOccurrences && recordOccurrences > 0){
				modeArray.push(uniqueNumberArray[index]);
			}
		}
	index = index + 1;	
	}
	var string = '';
	index = 0;
	if(modeArray.length == 0)
	{
		return "No mode";
	}
	else{
		while(index < modeArray.length){
			if (index > 0){
				string = string + ' ' + modeArray[index].toString();
			}
			else{
				string = modeArray[index].toString();
			}
			index = index + 1;
		}
		return string;
	}
}
		
function performStatistics()
{	
	var data = document.getElementById("data");
	var rawdata = data.value;
	var numbers = rawdata.split(" ");
	if (numbers.length < 5 || numbers.length > 20){
		alert("The calculator can only take five to twenty numbers.");
		return false;
	}
	index = 0;
	while (index < numbers.length){
		if (isNaN(numbers[index])){
			alert(numbers[index] + " is not a number.  Please enter a valid number.");
			return false;
		}
		else{
			numbers[index] = Number(numbers[index]);
			index = index + 1;
		}
	}
	numbers.sort(function(a, b){return a - b});
	if(findMin(numbers) < 0 || findMax(numbers) > 100){
		alert('All numbers have to be between 0 and 100.');
		return false;
	}
	document.getElementById("maximum").value = findMax(numbers).toFixed(2);
	document.getElementById("minimum").value = findMin(numbers).toFixed(2);
	document.getElementById("median").value = calcMedian(numbers).toFixed(2);
	document.getElementById("average").value = calcMean(numbers).toFixed(2);
	document.getElementById("sum").value = calcSum(numbers).toFixed(2);
	document.getElementById("mode").value = calcMode(numbers);
	document.getElementById("variance").value = calcVariance(numbers).toFixed(2);
	document.getElementById("stdDev").value = calcStdDev(numbers).toFixed(2);
	return false;
}