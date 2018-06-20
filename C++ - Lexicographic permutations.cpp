/*
A permutation is an ordered arrangement of objects. For example, 3124 is one possible permutation of the digits 1, 2, 3 and 4. 
If all of the permutations are listed numerically or alphabetically, we call it lexicographic order. 
The lexicographic permutations of 0, 1 and 2 are:

012   021   102   120   201   210

What is the millionth lexicographic permutation of the digits 0, 1, 2, 3, 4, 5, 6, 7, 8 and 9?
*/

#include <iostream>
#include <algorithm>
#define TARGET 1000000
#define DIGITS 10

using namespace std;

// Return the factorial of given integer.
int factorial(int n) {
	if (n > 1) { return n * factorial(n - 1); }
	else { return 1; }
}

int main()
{
	// Calculate the number of iterations (times a digit changes) each digit has in our target amount of permutations
	int iteration = TARGET - 1;
	int count = 1;
	int iterations[DIGITS] = { 0 };
	while (count <= DIGITS) {
		int fact = factorial(DIGITS - count);
		int divisions = iteration / fact;
		cout << "Number of iterations for digit " << count - 1 << " is: " << divisions << endl;
		iterations[count - 1] = divisions;
		iteration %= fact;
		count++;
	}
	cout << endl;

	// Determine the actual number each digit will have in our target permutation (the number of iterations + the number of digits we encounter that are already taken).
	for (int i = 0; i < DIGITS; i++) {
		for (int j = 0; j <= iterations[i]; j++) {
			if (find(iterations, iterations + i, j) != iterations + i) {
				iterations[i]++;
			}
		}
	}

	// Now print the final values:
	cout << "The " << TARGET << "th permutation of  these " << DIGITS << " digits is: ";
	for (int i = 0; i < DIGITS; i++) {
		cout << iterations[i];
	}
	cout << endl;
	return 0;
}
