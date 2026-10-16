// Experiment 4: Composite 2D transformations using matrix representation
#include <GL/glut.h>
#include <cmath>
typedef float M[3][3];
void mul(M a,M b,M c){for(int i=0;i<3;i++)for(int j=0;j<3;j++){c[i][j]=0;for(int k=0;k<3;k++)c[i][j]+=a[i][k]*b[k][j];}}
void draw(M m,float r,float g,float b){
    float p[5][2]={{-60,-50},{60,-50},{70,20},{0,70},{-70,20}};
    glColor3f(r,g,b);glBegin(GL_LINE_LOOP);
    for(auto &v:p){float x=m[0][0]*v[0]+m[0][1]*v[1]+m[0][2];float y=m[1][0]*v[0]+m[1][1]*v[1]+m[1][2];glVertex2f(x,y);}glEnd();
}
void display(){
    glClear(GL_COLOR_BUFFER_BIT);
    M T={{1,0,120},{0,1,20},{0,0,1}};
    float a=30*3.1415926f/180;
    M R={{cos(a),-sin(a),0},{sin(a),cos(a),0},{0,0,1}};
    M S={{0.8f,0,0},{0,1.2f,0},{0,0,1}}, RS, TRS;
    mul(R,S,RS); mul(T,RS,TRS);
    draw((M){{1,0,0},{0,1,0},{0,0,1}},0,0,0);
    draw(TRS,0,0,1);
    glFlush();
}
void init(){glClearColor(1,1,1,1);glMatrixMode(GL_PROJECTION);glLoadIdentity();gluOrtho2D(-300,300,-220,220);}
int main(int argc,char** argv){glutInit(&argc,argv);glutInitDisplayMode(GLUT_SINGLE|GLUT_RGB);glutInitWindowSize(800,600);glutCreateWindow("Experiment 4 - Composite Transformations");init();glutDisplayFunc(display);glutMainLoop();return 0;}
