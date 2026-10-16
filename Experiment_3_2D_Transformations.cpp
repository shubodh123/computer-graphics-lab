// Experiment 3: 2D transformations using homogeneous-coordinate matrices
#include <GL/glut.h>
#include <cmath>
typedef float M[3][3];
void multiply(M a,M b,M c) {
    for(int i=0;i<3;i++) for(int j=0;j<3;j++) {
        c[i][j]=0; for(int k=0;k<3;k++) c[i][j]+=a[i][k]*b[k][j];
    }
}
void apply(M m,float x,float y,float &ox,float &oy) {
    ox=m[0][0]*x+m[0][1]*y+m[0][2];
    oy=m[1][0]*x+m[1][1]*y+m[1][2];
}
void polygon(float r,float g,float b,M m) {
    float p[4][2]={{-60,-40},{60,-40},{60,40},{-60,40}};
    glColor3f(r,g,b); glBegin(GL_LINE_LOOP);
    for(auto &v:p){float x,y;apply(m,v[0],v[1],x,y);glVertex2f(x,y);} glEnd();
}
void display() {
    glClear(GL_COLOR_BUFFER_BIT);
    M identity={{1,0,0},{0,1,0},{0,0,1}};
    M translate={{1,0,130},{0,1,0},{0,0,1}};
    float a=35*3.1415926f/180;
    M rotate={{cos(a),-sin(a),0},{sin(a),cos(a),0},{0,0,1}};
    M scale={{1.3f,0,0},{0,0.7f,0},{0,0,1}};
    M reflect={{1,0,0},{0,-1,0},{0,0,1}};
    M shear={{1,0.5f,0},{0,1,0},{0,0,1}};
    polygon(0,0,0,identity); polygon(0,0,1,translate);
    polygon(1,0,0,rotate); polygon(0,0.6f,0,scale);
    polygon(0.7f,0,0.7f,reflect); polygon(0.9f,0.5f,0,shear);
    glFlush();
}
void init(){glClearColor(1,1,1,1);glMatrixMode(GL_PROJECTION);glLoadIdentity();gluOrtho2D(-300,300,-220,220);}
int main(int argc,char** argv){glutInit(&argc,argv);glutInitDisplayMode(GLUT_SINGLE|GLUT_RGB);glutInitWindowSize(800,600);glutCreateWindow("Experiment 3 - 2D Transformations");init();glutDisplayFunc(display);glutMainLoop();return 0;}
