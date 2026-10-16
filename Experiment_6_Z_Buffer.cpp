// Experiment 6: Z-buffer hidden-surface removal using OpenGL depth testing
#include <GL/glut.h>
void display(){
    glClear(GL_COLOR_BUFFER_BIT|GL_DEPTH_BUFFER_BIT);
    glMatrixMode(GL_MODELVIEW);glLoadIdentity();
    glTranslatef(0,0,-5);
    // Far triangle is drawn first.
    glColor3f(0.1f,0.3f,0.9f);
    glBegin(GL_TRIANGLES);glVertex3f(-1.5f,-1,0);glVertex3f(1.5f,-1,0);glVertex3f(0,1.5f,0);glEnd();
    // Near triangle overlaps it; depth buffer determines the visible surface.
    glColor3f(0.9f,0.2f,0.2f);
    glBegin(GL_TRIANGLES);glVertex3f(-0.8f,-0.6f,1);glVertex3f(1.2f,-0.6f,1);glVertex3f(0.2f,1.1f,1);glEnd();
    glutSwapBuffers();
}
void init(){
    glClearColor(1,1,1,1);glEnable(GL_DEPTH_TEST);glDepthFunc(GL_LESS);
    glMatrixMode(GL_PROJECTION);glLoadIdentity();gluPerspective(60,1.33,1,100);
}
int main(int argc,char** argv){glutInit(&argc,argv);glutInitDisplayMode(GLUT_DOUBLE|GLUT_RGB|GLUT_DEPTH);glutInitWindowSize(800,600);glutCreateWindow("Experiment 6 - Z Buffer");init();glutDisplayFunc(display);glutMainLoop();return 0;}
