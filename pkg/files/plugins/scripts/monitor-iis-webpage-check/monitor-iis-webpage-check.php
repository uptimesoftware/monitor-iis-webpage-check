<?php
// get all the variables from the environmental variable
$ssl			= getenv('UPTIME_NTLM_SSL');
$windows_domain	= getenv('UPTIME_UTDOMAIN');
$username		= getenv('UPTIME_UTUSER');
$password		= getenv('UPTIME_UTPASSWORD');
$hostname		= getenv('UPTIME_HOSTNAME');
$port			= getenv('UPTIME_WEBPORT');
$webpage		= getenv('UPTIME_WEBPAGE');
$virtualHost	= getenv('UPTIME_VIRTUALHOST');

// Determine URL string
if ($ssl == "false") {
	$url = "http://";
} else {
	$url = "https://";
}
if ($virtualHost == "") {
	$url = $url.$hostname.":".$port."/".$webpage;
} else {
	$url = $url.$virtualHost.":".$port."/".$webpage;
}

// Hit webpage
$curl_command = "curl.exe --ntlm -s -k -IL -u".$windows_domain."\\".$username.":".$password." ".$url;


exec($curl_command, $output, $returnCode);

// Find the line with HTTP response code
$httpResponse=preg_grep("/HTTP\/1\.1 ([0-9]{3}) .*/", $output);

// Print HTTP Code, ignore first code because it seems to always return 401
$firstItem = true;
foreach($httpResponse as $line) {
	if(!$firstItem) {
		$splitLine=preg_split("/ /",$line);
		$returnCode=$splitLine[1];
		echo "\noutput $line";
	} else {
		$firstItem = false;
	}
}

$allOutput = ""; 

if ($returnCode == 200) {
	$curl_command = "curl.exe --ntlm -s -k -L -u".$windows_domain."\\".$username.":".$password." ".$url;
	exec($curl_command, $output200, $returnCode);
	foreach($output200 as $line) {
		$allOutput = $allOutput ." ". $line;
	}
	$allOutput = substr($allOutput,0,65000);
	echo "output $allOutput";
	exit(0);
} else {
	exit(2);
}


?>
