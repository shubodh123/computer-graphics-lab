// Experiment 1: DDA and Bresenham Line Drawing Algorithms
// Build with a C++ compiler linked to OpenGL and GLUT/freeglut.
#include <GL/glut.h>
#include <cmath>
#include <cstdlib>

void pixel(int x, int y) {
    glBegin(GL_POINTS); glVertex2i(x, y); glEnd();
}
void dda(int x1, int y1, int x2, int y2) {
    int dx=x2-x1, dy=y2-y1;
    int steps = abs(dx) > abs(dy) ? abs(dx) : abs(dy);
    if (steps == 0) { pixel(x1,y1); return; }
    float x=x1, y=y1, xinc=(float)dx/steps, yinc=(float)dy/steps;
    for(int i=0;i<=steps;i++,x+=xinc,y+=yinc) pixel((int)round(x),(int)round(y));
}
void bresenham(int x1,int y1,int x2,int y2) {
    int dx=abs(x2-x1), sx=x1<x2?1:-1;
    int dy=-abs(y2-y1), sy=y1<y2?1:-1, err=dx+dy;
    while(true) {
        pixel(x1,y1);
        if(x1==x2 && y1==y2) break;
        int e2=2*err;
        if(e2>=dy){err+=dy;x1+=sx;}
        if(e2<=dx){err+=dx;y1+=sy;}
    }
}
void display() {
    glClear(GL_COLOR_BUFFER_BIT);
    glColor3f(0.1f,0.2f,0.9f);
    dda(-220,-100,220,100);
    glColor3f(0.9f,0.1f,0.1f);
    bresenham(-220,100,220,-100);
    glFlush();
}
void init() {
    glClearColor(1,1,1,1); glPointSize(2);
    glMatrixMode(GL_PROJECTION); glLoadIdentity();
    gluOrtho2D(-300,300,-200,200);
}
int main(int argc,char** argv) {
    glutInit(&argc,argv); glutInitDisplayMode(GLUT_SINGLE|GLUT_RGB);
    glutInitWindowSize(800,600); glutCreateWindow("Experiment 1 - DDA and Bresenham");
    init(); glutDisplayFunc(display); glutMainLoop(); return 0;
}
