// what is this monster ?? (reddit programmerHumor)
// src: https://www.reddit.com/r/ProgrammerHumor/comments/b7gayf/no_im_not_ashamed/
function IsPrime(n, i=5) { 
	return n<4?n>1:n%2&&n%3?i*i<n?n%i&&n%(i+2)?prime(n,i+6):0:1:0; 
}

// prime
// src: https://www.reddit.com/r/ProgrammerHumor/comments/b7gayf/no_im_not_ashamed/
function IsPrime(n) {
	if (n <= 3){
		return n > 1;
	}
	else if (n % 2 == 0 || n % 3 == 0){
		return false;
	}
	for (var i = 5; i*i <= n; i + 6){
		if (n % i == 0 || n % (i + 2) == 0){
			return false;
		}
	}
	return true;
}
