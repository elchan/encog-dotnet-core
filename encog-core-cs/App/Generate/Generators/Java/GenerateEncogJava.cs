﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Encog.App.Generate.Program;
using System.IO;
using Encog.ML;
using Encog.Persist;
using Encog.ML.Data;
using Encog.Util.Simple;
using Encog.Util.CSV;

namespace Encog.App.Generate.Generators.Java
{
    /// <summary>
    /// Generate Java.
    /// </summary>
    public class GenerateEncogJava : AbstractGenerator
    {
        private bool embed;

        private void EmbedNetwork(EncogProgramNode node)
        {
            AddBreak();

            FileInfo methodFile = (FileInfo)node.Args[0].Value;

            IMLMethod method = (IMLMethod)EncogDirectoryPersistence
                    .LoadObject(methodFile);

            if (!(method is IMLFactory))
            {
                throw new EncogError("Code generation not yet supported for: "
                        + method.GetType().Name);
            }

            IMLFactory factoryMethod = (IMLFactory)method;

            String methodName = factoryMethod.FactoryType;
            String methodArchitecture = factoryMethod.FactoryArchitecture;

            // header
            AddInclude("org.encog.ml.MLMethod");
            AddInclude("org.encog.persist.EncogDirectoryPersistence");

            StringBuilder line = new StringBuilder();
            line.Append("public static MLMethod ");
            line.Append(node.Name);
            line.Append("() {");
            IndentLine(line.ToString());

            // create factory
            line.Length = 0;
            AddInclude("org.encog.ml.factory.MLMethodFactory");
            line.Append("MLMethodFactory methodFactory = new MLMethodFactory();");
            AddLine(line.ToString());

            // factory create
            line.Length = 0;
            line.Append("MLMethod result = ");

            line.Append("methodFactory.create(");
            line.Append("\"");
            line.Append(methodName);
            line.Append("\"");
            line.Append(",");
            line.Append("\"");
            line.Append(methodArchitecture);
            line.Append("\"");
            line.Append(", 0, 0);");
            AddLine(line.ToString());

            line.Length = 0;
            AddInclude("org.encog.ml.MLEncodable");
            line.Append("((MLEncodable)result).decodeFromArray(WEIGHTS);");
            AddLine(line.ToString());

            // return
            AddLine("return result;");

            UnIndentLine("}");
        }

        private void EmbedTraining(EncogProgramNode node)
        {

            FileInfo dataFile = (FileInfo)node.Args[0].Value;
            IMLDataSet data = EncogUtility.LoadEGB2Memory(dataFile);

            // generate the input data

            IndentLine("public static final double[][] INPUT_DATA = {");
            foreach (IMLDataPair pair in data)
            {
                IMLData item = pair.Input;

                StringBuilder line = new StringBuilder();

                NumberList.ToList(CSVFormat.EgFormat, line, item);
                line.Insert(0, "{ ");
                line.Append(" },");
                AddLine(line.ToString());
            }
            UnIndentLine("};");

            AddBreak();

            // generate the ideal data

            IndentLine("public static final double[][] IDEAL_DATA = {");
            foreach (IMLDataPair pair in data)
            {
                IMLData item = pair.Ideal;

                StringBuilder line = new StringBuilder();

                NumberList.ToList(CSVFormat.EgFormat, line, item);
                line.Insert(0, "{ ");
                line.Append(" },");
                AddLine(line.ToString());
            }
            UnIndentLine("};");
        }

        public override void Generate(EncogGenProgram program, bool shouldEmbed)
        {
            this.embed = shouldEmbed;
            GenerateForChildren(program);
            GenerateImports(program);
        }

        private void GenerateArrayInit(EncogProgramNode node)
        {
            StringBuilder line = new StringBuilder();
            line.Append("public static final double[] ");
            line.Append(node.Name);
            line.Append(" = {");
            IndentLine(line.ToString());

            double[] a = (double[])node.Args[0].Value;

            line.Length = 0;

            int lineCount = 0;
            for (int i = 0; i < a.Length; i++)
            {
                line.Append(CSVFormat.EgFormat.Format(a[i],
                        EncogFramework.DefaultPrecision));
                if (i < (a.Length - 1))
                {
                    line.Append(",");
                }

                lineCount++;
                if (lineCount >= 10)
                {
                    AddLine(line.ToString());
                    line.Length = 0;
                    lineCount = 0;
                }
            }

            if (line.Length > 0)
            {
                AddLine(line.ToString());
                line.Length = 0;
            }

            UnIndentLine("};");
        }

        private void GenerateClass(EncogProgramNode node)
        {
            AddBreak();
            IndentLine("public class " + node.Name + " {");
            GenerateForChildren(node);
            UnIndentLine("}");
        }

        private void GenerateComment(EncogProgramNode commentNode)
        {
            AddLine("// " + commentNode.Name);
        }

        private void GenerateConst(EncogProgramNode node)
        {
            StringBuilder line = new StringBuilder();
            line.Append("public static final ");
            line.Append(node.Args[1].Value);
            line.Append(" ");
            line.Append(node.Name);
            line.Append(" = \"");
            line.Append(node.Args[0].Value);
            line.Append("\";");

            AddLine(line.ToString());
        }

        private void GenerateCreateNetwork(EncogProgramNode node)
        {
            if (this.embed)
            {
                EmbedNetwork(node);
            }
            else
            {
                LinkNetwork(node);
            }
        }

        private void GenerateEmbedTraining(EncogProgramNode node)
        {
            if (this.embed)
            {
                EmbedTraining(node);
            }
        }

        private void GenerateForChildren(EncogTreeNode parent)
        {
            foreach (EncogProgramNode node in parent.Children)
            {
                GenerateNode(node);
            }
        }

        private void GenerateFunction(EncogProgramNode node)
        {
            AddBreak();

            StringBuilder line = new StringBuilder();
            line.Append("public static void ");
            line.Append(node.Name);
            line.Append("() {");
            IndentLine(line.ToString());

            GenerateForChildren(node);
            UnIndentLine("}");
        }

        private void GenerateFunctionCall(EncogProgramNode node)
        {
            AddBreak();
            StringBuilder line = new StringBuilder();
            if (node.Args[0].Value.ToString().Length > 0)
            {
                line.Append(node.Args[0].Value.ToString());
                line.Append(" ");
                line.Append(node.Args[1].Value.ToString());
                line.Append(" = ");
            }

            line.Append(node.Name);
            line.Append("();");
            AddLine(line.ToString());
        }

        private void GenerateImports(EncogGenProgram program)
        {
            StringBuilder imports = new StringBuilder();
            foreach (String str in Includes)
            {
                imports.Append("import ");
                imports.Append(str);
                imports.Append(";\n");
            }

            imports.Append("\n");

            AddToBeginning(imports.ToString());

        }

        private void GenerateLoadTraining(EncogProgramNode node)
        {
            AddBreak();

            FileInfo methodFile = (FileInfo)node.Args[0].Value;

            AddInclude("org.encog.ml.data.MLDataSet");
            StringBuilder line = new StringBuilder();
            line.Append("public static MLDataSet createTraining() {");
            IndentLine(line.ToString());

            line.Length = 0;

            if (this.embed)
            {
                AddInclude("org.encog.ml.data.basic.BasicMLDataSet");
                line.Append("MLDataSet result = new BasicMLDataSet(INPUT_DATA,IDEAL_DATA);");
            }
            else
            {
                AddInclude("org.encog.util.simple.EncogUtility");
                line.Append("MLDataSet result = EncogUtility.loadEGB2Memory(new File(\"");
                line.Append(methodFile.ToString());
                line.Append("\"));");
            }

            AddLine(line.ToString());

            // return
            AddLine("return result;");

            UnIndentLine("}");
        }

        private void GenerateMainFunction(EncogProgramNode node)
        {
            AddBreak();
            IndentLine("public static void main(String[] args) {");
            GenerateForChildren(node);
            UnIndentLine("}");
        }

        private void GenerateNode(EncogProgramNode node)
        {
            switch (node.Type)
            {
                case NodeType.Comment:
                    GenerateComment(node);
                    break;
                case NodeType.Class:
                    GenerateClass(node);
                    break;
                case NodeType.MainFunction:
                    GenerateMainFunction(node);
                    break;
                case NodeType.Const:
                    GenerateConst(node);
                    break;
                case NodeType.StaticFunction:
                    GenerateFunction(node);
                    break;
                case NodeType.FunctionCall:
                    GenerateFunctionCall(node);
                    break;
                case NodeType.CreateNetwork:
                    GenerateCreateNetwork(node);
                    break;
                case NodeType.InitArray:
                    GenerateArrayInit(node);
                    break;
                case NodeType.EmbedTraining:
                    GenerateEmbedTraining(node);
                    break;
                case NodeType.LoadTraining:
                    GenerateLoadTraining(node);
                    break;
            }
        }

        private void LinkNetwork(EncogProgramNode node)
        {
            AddBreak();

            FileInfo methodFile = (FileInfo)node.Args[0].Value;

            AddInclude("org.encog.ml.MLMethod");
            StringBuilder line = new StringBuilder();
            line.Append("public static MLMethod ");
            line.Append(node.Name);
            line.Append("() {");
            IndentLine(line.ToString());

            line.Length = 0;
            line.Append("MLMethod result = (MLMethod)EncogDirectoryPersistence.loadObject(new File(\"");
            line.Append(methodFile.ToString());
            line.Append("\"));");
            AddLine(line.ToString());

            // return
            AddLine("return result;");

            UnIndentLine("}");
        }
    }
}
