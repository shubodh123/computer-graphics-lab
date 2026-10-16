// Experiment 2: Midpoint Circle Drawing Algorithm
#include <GL/glut.h>
void plot8(int xc,int yc,int x,int y) {
    glBegin(GL_POINTS);
    glVertex2i(xc+x,yc+y); glVertex2i(xc-x,yc+y);
    glVertex2i(xc+x,yc-y); glVertex2i(xc-x,yc-y);
    glVertex2i(xc+y,yc+x); glVertex2i(xc-y,yc+x);
    glVertex2i(xc+y,yc-x); glVertex2i(xc-y,yc-x);
    glEnd();
}
void circle(int xc,int yc,int r) {
    int x=0,y=r,p=1-r;
    while(x<=y) {
        plot8(xc,yc,x,y); x++;
        if(p<0) p+=2*x+1;
        else { y--; p+=2*(x-y)+1; }
    }
}
void display() {
    glClear(GL_COLOR_BUFFER_BIT); glColor3f(0,0,0);
    circle(0,0,130); glFlush();
}
void init() {
    glClearColor(1,1,1,1); glPointSize(2);
    glMatrixMode(GL_PROJECTION); glLoadIdentity(); gluOrtho2D(-250,250,-200,200);
}
int main(int argc,char** argv) {
    glutInit(&argc,argv); glutInitDisplayMode(GLUT_SINGLE|GLUT_RGB);
    glutInitWindowSize(700,600); glutCreateWindow("Experiment 2 - Midpoint Circle");
    init(); glutDisplayFunc(display); glutMainLoop(); return 0;
}
