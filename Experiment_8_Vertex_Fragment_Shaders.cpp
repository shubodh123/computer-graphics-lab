// Experiment 8: Basic OpenGL program using vertex and fragment shaders.
// Requires GLEW and FreeGLUT. Create a compatibility context that supports GLSL 1.20.
#include <GL/glew.h>
#include <GL/freeglut.h>
#include <iostream>
const char* vertexShaderSource =
    "#version 120\n"
    "attribute vec2 position;\n"
    "varying vec3 color;\n"
    "void main(){ gl_Position=vec4(position,0.0,1.0); color=vec3((position.x+1.0)/2.0,0.4,(position.y+1.0)/2.0); }\n";
const char* fragmentShaderSource =
    "#version 120\n"
    "varying vec3 color;\n"
    "void main(){ gl_FragColor=vec4(color,1.0); }\n";
GLuint program, vbo;
GLuint compileShader(GLenum type,const char* src){
    GLuint s=glCreateShader(type);glShaderSource(s,1,&src,nullptr);glCompileShader(s);
    GLint ok;glGetShaderiv(s,GL_COMPILE_STATUS,&ok);
    if(!ok){char log[1024];glGetShaderInfoLog(s,1024,nullptr,log);std::cerr<<log<<std::endl;}
    return s;
}
void display(){
    glClear(GL_COLOR_BUFFER_BIT);
    glUseProgram(program);
    glBindBuffer(GL_ARRAY_BUFFER,vbo);
    GLint loc=glGetAttribLocation(program,"position");
    glEnableVertexAttribArray(loc);glVertexAttribPointer(loc,2,GL_FLOAT,GL_FALSE,0,nullptr);
    glDrawArrays(GL_TRIANGLES,0,3);
    glDisableVertexAttribArray(loc);glUseProgram(0);glutSwapBuffers();
}
void init(){
    glewInit();
    GLuint vs=compileShader(GL_VERTEX_SHADER,vertexShaderSource);
    GLuint fs=compileShader(GL_FRAGMENT_SHADER,fragmentShaderSource);
    program=glCreateProgram();glAttachShader(program,vs);glAttachShader(program,fs);glLinkProgram(program);
    GLfloat vertices[]={-0.7f,-0.6f,0.7f,-0.6f,0.0f,0.7f};
    glGenBuffers(1,&vbo);glBindBuffer(GL_ARRAY_BUFFER,vbo);glBufferData(GL_ARRAY_BUFFER,sizeof(vertices),vertices,GL_STATIC_DRAW);
    glClearColor(0.08f,0.08f,0.1f,1);
}
int main(int argc,char** argv){
    glutInit(&argc,argv);glutInitDisplayMode(GLUT_DOUBLE|GLUT_RGB);glutInitWindowSize(800,600);
    glutCreateWindow("Experiment 8 - Vertex and Fragment Shaders");init();glutDisplayFunc(display);glutMainLoop();return 0;
}
