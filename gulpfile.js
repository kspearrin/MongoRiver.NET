var p = require('./package.json')
    gulp = require('gulp'),
    assemblyInfo = require('gulp-dotnet-assembly-info')
    xmlpoke = require('gulp-xmlpoke')
    msbuild = require('gulp-msbuild')
    nuget = require('nuget-runner')({
        apiKey: process.env.NUGET_API_KEY,
        nugetPath: '.nuget/nuget.exe'
    });

gulp.task('default', ['nuget']);

gulp.task('assemblyInfo', [], function() {
    return gulp
        .src('src/SharedAssemblyInfo.cs')
        .pipe(assemblyInfo({
            version: p.version,
            title: p.name,
            description: p.description, 
            configuration: 'Release', 
            company: p.author, 
            product: p.name,
            copyright: 'Copyright (C) ' + p.author + ' ' + new Date().getFullYear()
        }))
        .pipe(gulp.dest('src'));
});

gulp.task('restore', ['assemblyInfo'], function() {
    return nuget
        .restore({
            packages: 'MongoRiver.NET.sln', 
            verbosity: 'normal'
        });
});

gulp.task('build', ['restore'], function() {
    return gulp
        .src('MongoRiver.NET.sln')
        .pipe(msbuild({
            toolsVersion: 12.0,
            targets: ['Clean', 'Build'],
            errorOnFail: true,
            configuration: 'Release'
        }));
});

gulp.task('nuspec', ['build'], function() {
    return gulp
        .src('MongoRiver.NET.nuspec')
        .pipe(xmlpoke({
            replacements : [{
                xpath : "//package:version",
                namespaces : { "package" : "http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd" },
                value: p.version
            }]
        }))
        .pipe(gulp.dest('.'));
});

gulp.task('nuget', ['nuspec'], function() {
    return nuget
        .pack({
            spec: 'MongoRiver.NET.nuspec',
            outputDirectory: 'src/MongoRiver/bin/Release',
            version: p.version
        });
});
