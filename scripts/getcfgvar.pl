#!/usr/bin/perl -w

use XML::XPath;

if (@ARGV != 1) {
	print STDERR "Usage: $0 query\n";
	exit 1;
}

my $xpath;
my $nodes;
my $filename;
my $home = $ENV {"HOME"};

if (-e "MonkeyWrench.xml") {
	$filename = "MonkeyWrench.xml";
} elsif (-e "$home/.config/MonkeyWrench/MonkeyWrench.xml") {
	$filename = "$home/.config/MonkeyWrench/MonkeyWrench.xml";
} elsif (-e "/etc/MonkeyWrench.xml") {
	$filename = "/etc/MonkeyWrench.xml";
} else {
	printf STDERR "Could not find MonkeyWrench.xml\n";
	exit 1;
}

#print "Found configuration file: $filename \n";

$xpath = XML::XPath->new($filename);
$nodes = $xpath->find (shift @ARGV);

if ($nodes->size > 1) {
	print STDERR "More than one nodes found";
} elsif ($nodes->size) {
	foreach my $node ($nodes->get_nodelist) {
		print $node->string_value;
	}
} else {
	print STDERR "No nodes found";
}

print "\n";

exit;
