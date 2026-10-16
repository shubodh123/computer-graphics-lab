// Experiment 5: Cohen-Sutherland line clipping
#include <GL/glut.h>
#include <iostream>
using namespace std;
const int LEFT=1, RIGHT=2, BOTTOM=4, TOP=8;
const float xmin=-100,xmax=100,ymin=-80,ymax=80;
int code(float x,float y){int c=0;if(x<xmin)c|=LEFT;else if(x>xmax)c|=RIGHT;if(y<ymin)c|=BOTTOM;else if(y>ymax)c|=TOP;return c;}
bool clip(float &x1,float &y1,float &x2,float &y2){
    int c1=code(x1,y1),c2=code(x2,y2);
    while(true){
        if(!(c1|c2))return true;
        if(c1&c2)return false;
        int out=c1?c1:c2;float x,y;
        if(out&TOP){x=x1+(x2-x1)*(ymax-y1)/(y2-y1);y=ymax;}
        else if(out&BOTTOM){x=x1+(x2-x1)*(ymin-y1)/(y2-y1);y=ymin;}
        else if(out&RIGHT){y=y1+(y2-y1)*(xmax-x1)/(x2-x1);x=xmax;}
        else {y=y1+(y2-y1)*(xmin-x1)/(x2-x1);x=xmin;}
        if(out==c1){x1=x;y1=y;c1=code(x1,y1);}else{x2=x;y2=y;c2=code(x2,y2);}
    }
}
void display(){
    glClear(GL_COLOR_BUFFER_BIT);
    glColor3f(0,0,0);glBegin(GL_LINE_LOOP);glVertex2f(xmin,ymin);glVertex2f(xmax,ymin);glVertex2f(xmax,ymax);glVertex2f(xmin,ymax);glEnd();
    float x1=-180,y1=-130,x2=170,y2=130;
    glColor3f(0.8f,0.2f,0.2f);glBegin(GL_LINES);glVertex2f(x1,y1);glVertex2f(x2,y2);glEnd();
    if(clip(x1,y1,x2,y2)){glColor3f(0,0.5f,0);glBegin(GL_LINES);glVertex2f(x1,y1);glVertex2f(x2,y2);glEnd();}
    glFlush();
}
void init(){glClearColor(1,1,1,1);glMatrixMode(GL_PROJECTION);glLoadIdentity();gluOrtho2D(-220,220,-170,170);}
int main(int argc,char** argv){glutInit(&argc,argv);glutInitDisplayMode(GLUT_SINGLE|GLUT_RGB);glutInitWindowSize(800,600);glutCreateWindow("Experiment 5 - Cohen-Sutherland Clipping");init();glutDisplayFunc(display);glutMainLoop();return 0;}
