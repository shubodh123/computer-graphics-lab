// Experiment 7: Phong illumination model on a 3D sphere.
// Uses the OpenGL fixed-function lighting pipeline with smooth shading.
#include <GL/glut.h>
void display(){
    glClear(GL_COLOR_BUFFER_BIT|GL_DEPTH_BUFFER_BIT);
    glMatrixMode(GL_MODELVIEW);glLoadIdentity();gluLookAt(0,0,5,0,0,0,0,1,0);
    GLfloat pos[]={2,3,4,1}, ambient[]={0.15f,0.15f,0.15f,1};
    GLfloat diffuse[]={0.7f,0.5f,0.3f,1}, specular[]={1,1,1,1};
    GLfloat matA[]={0.2f,0.2f,0.2f,1}, matD[]={0.2f,0.6f,0.9f,1}, matS[]={1,1,1,1};
    GLfloat shininess[]={80};
    glLightfv(GL_LIGHT0,GL_POSITION,pos);glLightfv(GL_LIGHT0,GL_AMBIENT,ambient);
    glLightfv(GL_LIGHT0,GL_DIFFUSE,diffuse);glLightfv(GL_LIGHT0,GL_SPECULAR,specular);
    glMaterialfv(GL_FRONT,GL_AMBIENT,matA);glMaterialfv(GL_FRONT,GL_DIFFUSE,matD);
    glMaterialfv(GL_FRONT,GL_SPECULAR,matS);glMaterialfv(GL_FRONT,GL_SHININESS,shininess);
    glEnable(GL_LIGHTING);glEnable(GL_LIGHT0);glEnable(GL_DEPTH_TEST);glShadeModel(GL_SMOOTH);
    glutSolidSphere(1.3,60,60);glutSwapBuffers();
}
void init(){glClearColor(0.05f,0.05f,0.08f,1);glMatrixMode(GL_PROJECTION);glLoadIdentity();gluPerspective(45,1.33,1,100);}
int main(int argc,char** argv){glutInit(&argc,argv);glutInitDisplayMode(GLUT_DOUBLE|GLUT_RGB|GLUT_DEPTH);glutInitWindowSize(800,600);glutCreateWindow("Experiment 7 - Illumination Model");init();glutDisplayFunc(display);glutMainLoop();return 0;}
