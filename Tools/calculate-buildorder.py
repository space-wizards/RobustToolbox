'''
Build Order Calculator for Prebuild projects.  Run in root of SS14 folder.

(c)2015 Rob "N3X15" Nelson <nexisentertainment@gmail.com>

MIT License

Requires lxml. (pip install lxml)
'''
import os, sys
from lxml import etree

class BaseNode:
    def setAttr(self, element, name, skip_when_default=True, default=None):
        value = getattr(self, name, default)
        if value == default and skip_when_default: return 
        element.set(name, value)
            
    def getAttr(self, element, name, required=False, default=None):
        value = element.get(name, None)
        if value is None:
            if required:
                raise ValueError('{} is not set, but is required.'.format(name))
            else:
                value = default
        setattr(self, name, value)

class Configuration(BaseNode):
    def __init__(self):
        self.options = {}
        self.name = ''
        
    def SerializeXML(self, parent):
        config = etree.SubElement(parent, 'Configuration', {'name':self.name})
        options = etree.SubElement(config, 'Options')
        for k, v in self.options.items():
            etree.SubElement(options, k).text = v
            
    @classmethod    
    def DeserializeXML(cls, element):
        assert element.tag == 'Configuration'
        options = element[0]
        
        cfg = Configuration()
        for option in options:
            cfg.options[option.tag] = option.text
        return cfg

class Reference(BaseNode):
    def __init__(self):
        self.name = ''
        self.path = None
        
    def IsProjectReference(self):
        return self.path is None

    def SerializeXML(self, parent):
        ref = etree.SubElement(parent, "Reference", {'name':name})
        self.setAttr(ref, 'path')
    
    @classmethod    
    def DeserializeXML(cls, element):
        assert element.tag == 'Reference'
        ref = Reference()
        ref.getAttr(element, 'name', required=True)
        ref.getAttr(element, 'path')
        return ref
        
class File(BaseNode):
    def __init__(self):
        self.path = ''
        self.buildAction = None
        self.filename = ''
    def SerializeXML(self, parent):
        f = etree.SubElement(self.files, 'File')
        f.text = self.filename
        self.setAttr(element, 'path')
        self.setAttr(element, 'buildAction', default='Compile')
    
    @classmethod    
    def DeserializeXML(cls, element):
        assert element.tag == 'File'
        f = File()
        f.filename = element.text
        f.getAttr(element, 'path')
        f.getAttr(element, 'buildAction')
    
class Project(BaseNode):
    def __init__(self):
        self.name = ''
        self.frameworkVersion = ''
        self.rootNamespace = ''
        self.type = ''
        self.path = ''
        
        self.configurations = []
        self.referencePaths = []
        self.files = []
        self.references = []
        
    @classmethod
    def DeserializeXML(cls, element):
        assert element.tag == 'Project'
        
        proj = Project()
        proj.getAttr(element, 'frameworkVersion', required=True)
        proj.getAttr(element, 'name', required=True)
        proj.getAttr(element, 'type', required=True)
        proj.getAttr(element, 'path', required=True)
        
        proj.getAttr(element, 'rootNamespace')
        
        for child in element:
            if not etree.iselement(child) or child.tag is etree.Comment: continue
            if child.tag == 'Configuration':
                proj.configurations += [Configuration.DeserializeXML(child)]
            elif child.tag == 'ReferencePath':
                proj.referencePaths += [child.text]
            elif child.tag == 'Files':
                for filedata in child:
                    if not etree.iselement(filedata) or filedata.tag is etree.Comment: continue
                    proj.files += [File.DeserializeXML(filedata)]
            elif child.tag == 'Reference':
                proj.references += [Reference.DeserializeXML(child)]
            else:
                print('!!! Unknown project tag child {}'.format(child.tag))
        
        return proj
                
    def GetProjectDependencies(self):
        deps = []
        for ref in self.references:
            if ref.path is None:
                deps += [ref.name]
        return deps
        
    def SerializeXML(self):
        # <Project frameworkVersion="v4_0" name="Client" path="SS3D_Client" type="WinExe" rootNamespace="SS13">
        proj = etree.Element('Project')
        self.setAttr(proj, 'frameworkVersion', required=True)
        self.setAttr(proj, 'name', required=True)
        self.setAttr(proj, 'type', required=True)
        self.setAttr(proj, 'path', required=True)
        
        self.setAttr(proj, 'rootNamespace')
        
        for configuration in self.configurations:
            configuration.SerializeXML(self.tree)
        
        for refpath in self.referencePaths:
            etree.SubElement(self.tree, 'ReferencePath').text = refpath
        
        if len(self.files) > 0:
            files = etree.SubElement(self.tree, 'Files')
            for file in self.files:
                file.SerializeXML(files)
                
        if len(self.references) > 0:
            for ref in self.references:
                ref.SerializeXML(files)
        return f
    
    @classmethod
    def Load(cls, filename):
        proj = None
        with open(filename, 'r') as f:
            tree = etree.parse(f)
            root = tree.getroot()
            for elem in root.getiterator():
                if not hasattr(elem.tag, 'find'): continue  # (1)
                i = elem.tag.find('}')
                if i >= 0:
                    elem.tag = elem.tag[i + 1:]
            proj = Project.DeserializeXML(root)
        return proj
    
    def Save(self, filename):
        with open(filename, 'w') as f:
            f.write(etree.tostring(self.tree, pretty_print=True, encoding='utf-8'))
        print(' -> {}'.format(filename))

if __name__ == '__main__':
    projects = {}
    project_deps = {}
    build_order = []
    longestPath=0
    
    for entry in os.listdir('.'):
        if os.path.isdir(entry):
            for f in os.listdir(entry):
                filename = os.path.join(entry, f)
                if os.path.isfile(filename) and filename.endswith('prebuild.xml'):
                    print('Loading {}...'.format(filename))
                    proj = Project.Load(filename)
                    print('  Loaded {} project.'.format(proj.name))
                    projects[proj.name] = proj
                    project_deps[proj.name] = proj.GetProjectDependencies()
                    pathLen=len(proj.path)
                    if pathLen>longestPath:
                        longestPath=pathLen
                    
    for projName in project_deps.keys():
        deps = project_deps[projName]
        newDeps = []
        for dep in deps:
            if dep in projects:
                newDeps += [dep]
        project_deps[projName] = newDeps
                        
    projsLeft = len(projects)
        
    it = 0
    while projsLeft > 0:
        it += 1
        for projName in projects.keys():
            if projName in build_order: continue
            deps = project_deps[projName]
            if len(deps) == 0:
                build_order += [projName]
                projsLeft -= 1
                print('[{}] Added {} (0 deps)'.format(it, projName))
                continue
        
            defer = False
            for dep in deps:
                if dep not in build_order:
                    defer = True
                    break
            if defer: continue
            build_order += [projName]
            projsLeft -= 1
            print('[{}] Added {} ({} deps)'.format(it, projName, len(deps)))
    print('')
    print('\t\t<!-- BUILD ORDER CALCULATED BY Tools\calculate-buildorder.py -->')
    for projName in build_order:
        project = projects[projName]
        deps = project_deps[projName]
        # print(repr(deps))
        prereq = ''
        if len(deps) > 0:
            prereq = (' '*(longestPath-len(project.path))) + '<!-- Prerequisites: {} -->'.format(', '.join(sorted(deps)))
        print('\t\t<?include file="./{}/prebuild.xml" ?>{}'.format(project.path, prereq))
