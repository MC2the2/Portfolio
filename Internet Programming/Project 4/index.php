<?php
    session_start();
    require_once '/home/common/mail.php';
    processPageRequest(); 

    function addMovieToCart($movieID) 
	{
        $file = fopen('./data/cart.db', 'r') or die('Error reading movie data.');
        $movies = array();
        while(!feof($file)) 
		{
            $movie = trim(fgets($file));
            if(!empty($movie)) 
			{
                $movies[] = $movie;
            }
        }
        fclose($file);
		$movies[] = $movieID;
        
		$file = fopen('./data/cart.db', 'w') or die('Error writing movie data.');
        foreach($movies as $movie) 
		{
            fwrite($file, $movie . PHP_EOL);
        }
        fclose($file);
		
		displayCart();
    }

    function checkout($name, $address) 
	{
        $file = fopen('./data/cart.db', 'r') or die('Error reading movie data.');
        $movies = array();
        while(!feof($file))
		{
            $movie = trim(fgets($file));
            if(!empty($movie))
			{
                $movies[] = $movie;
            }
        }
        fclose($file);
		
		$message = "
		<!DOCTYPE html>
		<html>
			<body>
				<h2>myMovies Xpress!</h2>
				<p>This is your receipt for " . count($movies) . " movies you purchased from myMovies Express!</p>\n";
				
				if(count($movies) == 0):
					$message .= "<p>No movies purchased!</p>\n";
				else:
					$message .= "
					<table>
						<th>Poster</th>
						<th>Title(year)</th>\n";
					foreach($movies as $movieID): 
						$result = file_get_contents('http://www.omdbapi.com/?apikey=1eb23cd5&i=' . $movieID .'&type=movie&r=json');
						$movie = json_decode($result, true);
						
						$message .= "
						<tr>
							<td><img src='" . $movie['Poster'] . "' height='100'></td>
							<td><a href='https://www.imdb.com/title/" . $movieID ."' target='_blank'>" . $movie['Title'] . " (" . $movie['Year'] . ")</a></td>
						</tr>\n";
					endforeach;
					$message .= "</table>\n";
				endif;
			$message .= "
			</body>
		</html>\n";
		echo $message;
        $result = sendMail([email_id], $address, $name, 'myMovies Express! Receipt', $message);
		$resultMessage = "";
		switch($result) 
        {
            case 0:
                $resultMessage = "A receipt was sent to " . $address;
                break;
            case -1:
                $resultMessage = "An error occurred while sending a receipt to your email address";
                break;
            case -2:
                $resultMessage = "An invalid Mail ID value exists";
                break;
            case -3:
                $resultMessage = "An error occurred while accessing the database";
                break;
            default:
                $resultMessage = "Cannot send an email for another " . $result . " seconds";
        }
		header('Location: ./logon.php?message=' . urlencode($resultMessage));
    }

    function displayCart() 
	{
		$file = fopen('./data/cart.db', 'r') or die('Error reading movie data.');
        $movies = array();
        while(!feof($file)) 
		{
            $movie = trim(fgets($file));
            if(!empty($movie)) 
			{
                $movies[] = $movie;
            }
        }
        fclose($file);
		require_once './templates/cart_form.html';
    }

    function processPageRequest() 
	{
        if(isset($_GET['action']))
		{
			if($_GET['action'] == 'add')
			{
				addMovieToCart($_GET['movie_id']);
			}
			else if($_GET['action'] == 'checkout')
			{
				checkout($_SESSION['display_name'], $_SESSION['email_address']);
			}
			elseif($_GET['action'] == 'remove') 
			{
				removeMovieFromCart($_GET['movie_id']);
			} 
			else 
			{
				displayCart();
			}
        } 
		else
		{
            displayCart();
        }
    }

    function removeMovieFromCart($movieID) 
	{
        $file = fopen('./data/cart.db', 'r') or die('Error reading movie data.');
        $movies = array();
        while(!feof($file)) 
		{
            $movie = trim(fgets($file));
            if(!empty($movie)) 
			{
                $movies[] = $movie;
            }
        }
        fclose($file);
		
		$index = array_search($movieID, $movies);
        if($index !== false) 
		{
            array_splice($movies, $index, 1);
			
            $file = fopen('./data/cart.db', 'w') or die('Error writing movie data.');
			foreach($movies as $movie) 
			{
				fwrite($file, $movie . PHP_EOL);
			}
			fclose($file);
        }
		
		displayCart();
    }
?>