// Find the number of paths through a grid, being only allowed to go right or down.
#include <iostream>

#define GRIDHEIGHT 20
#define GRIDWIDTH 20

using namespace std;

// Array we will store intermediate results in to speed up execution
long long results[GRIDHEIGHT][GRIDWIDTH] = {0};

// Recursive function that returns our results
long long Lattice(int height, int width){
    // If height or width is zero there is only one path. Return 1.
    if (height == 0 || width == 0){
        return 1;
    }
    // Otherwise, if we already have a stored value for this width/height combination, return that.
    else if (results[height-1][width-1] != 0){
        return results[height-1][width-1];
    }
    // Otherwise we compute the value for this width/height combination, store it, then return it.
    else{
        long long r_val = Lattice(height -1, width) + Lattice(height, width - 1);
        results[height-1][width-1] = r_val;
        return r_val;
    }
}

int main()
{
    long long result = Lattice(GRIDHEIGHT, GRIDWIDTH);
    cout<<"The total number of routes through a "<<GRIDHEIGHT<<"x"<<GRIDWIDTH<<" grid is "<<result;
    return 0;
}
